#!/usr/bin/env python3
"""Drive every flow a scaffolded provider template offers, the way a browser and its clients would.

Usage: drive-provider.py <issuer> <login> <password>

The host must run in Development, where appsettings.Development.json declares test_client and
console_client with the published secret "secret". Each check prints one line; the script exits 1 when
any failed, after running the rest, so one run names every broken flow.

The pages are read with html.parser, never by matching their text: the login form's antiforgery field
and the sign-out forms are found as the form elements they are, whatever their markup looks like.
"""
import http.cookiejar
import json
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request
from html.parser import HTMLParser

issuer, login, password = sys.argv[1], sys.argv[2], sys.argv[3]

# The host serves the ASP.NET Core development certificate, which the runner does not trust
tls = ssl.create_default_context()
tls.check_hostname = False
tls.verify_mode = ssl.CERT_NONE


class KeepRedirects(urllib.request.HTTPRedirectHandler):
    """Hands every redirect back to the caller, which checks where it points before following it."""

    def redirect_request(self, *args, **kwargs):
        return None


def browser():
    """A client with its own cookie jar, as a separate browser would have."""
    return urllib.request.build_opener(urllib.request.HTTPSHandler(context=tls),
                                       urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()),
                                       KeepRedirects)


def call(client, url, form=None, headers=None):
    request = urllib.request.Request(urllib.parse.urljoin(issuer + "/", url),
                                     data=urllib.parse.urlencode(form).encode() if form is not None else None,
                                     headers=headers or {})
    try:
        with client.open(request) as response:
            return response.status, response.headers.get("Location", ""), response.read().decode()
    except urllib.error.HTTPError as error:
        return error.code, error.headers.get("Location", ""), error.read().decode()


class Page(HTMLParser):
    """The forms of a page with their hidden fields, its links, the text of its elements by id, and its local assets."""

    def __init__(self, html):
        super().__init__()
        self.forms, self.text, self.assets, self.links = [], {}, [], []
        self._form = self._id = self._id_tag = None
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        attributes = dict(attrs)
        if tag == "form":
            self._form = {"action": attributes.get("action") or "", "fields": {}}
            self.forms.append(self._form)
        elif tag == "input" and self._form is not None and attributes.get("type") == "hidden":
            self._form["fields"][attributes["name"]] = attributes.get("value") or ""
        asset = attributes.get("href") if tag == "link" else attributes.get("src") if tag in ("script", "img") else None
        if asset and asset.startswith("/"):
            self.assets.append(asset)
        if tag == "a" and attributes.get("href"):
            self.links.append(attributes["href"])
        if "id" in attributes:
            self._id, self._id_tag = attributes["id"], tag

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)

    def handle_endtag(self, tag):
        if tag == "form":
            self._form = None
        if self._id is not None and tag == self._id_tag:
            self._id = None

    def handle_data(self, data):
        if self._id is not None:
            self.text[self._id] = self.text.get(self._id, "") + data


failures = []


def check(name, passed, detail):
    print(("ok   " if passed else "FAIL ") + name + ("" if passed else f": {detail}"))
    if not passed:
        failures.append(name)
    return passed


def redirect(status):
    return status in (302, 303)


plain = browser()

status, _, body = call(plain, "/.well-known/openid-configuration")
check("discovery names this issuer", status == 200 and json.loads(body).get("issuer") == issuer, f"{status} {body[:200]}")
status, _, body = call(plain, "/.well-known/jwks")
check("jwks serves one signing key", status == 200 and len(json.loads(body).get("keys", [])) == 1, f"{status} {body[:200]}")

status, _, body = call(plain, "/connect/token", {"grant_type": "client_credentials", "client_id": "console_client",
                                                 "client_secret": "secret"})
if check("client credentials grant issues a token to console_client", status == 200, f"{status} {body[:300]}"):
    status, _, body = call(plain, "/connect/introspect", {"token": json.loads(body)["access_token"],
                                                          "client_id": "console_client", "client_secret": "secret"})
    check("introspection reports that token active", status == 200 and json.loads(body).get("active") is True,
          f"{status} {body[:300]}")

status, _, body = call(plain, "/")
home = Page(body)
check("home page offers the demo client", status == 200 and "/demo/start" in home.links, f"{status} {body[:200]}")
for asset in home.assets:
    status, _, _ = call(plain, asset)
    check(f"home page asset {asset} loads", status == 200, status)


def sign_in(client):
    """A sign-in as test_client through the demo client, from its start page to its callback page."""
    status, location, body = call(client, "/demo/start")
    if not check("demo client sends the browser to authorize", redirect(status) and "/connect/authorize" in location,
                 f"{status} {location} {body[:200]}"):
        return None
    status, location, body = call(client, location)
    if not check("authorization request asks for the login page", redirect(status) and "/Auth/Login" in location,
                 f"{status} {location} {body[:300]}"):
        return None
    status, _, body = call(client, location)
    forms = Page(body).forms
    if not check("login page shows its form", status == 200 and len(forms) == 1, f"{status} {body[:300]}"):
        return None
    status, location, body = call(client, forms[0]["action"], dict(forms[0]["fields"], email=login, password=password))
    if not check("sign-in resumes the authorization request", redirect(status) and "/connect/authorize" in location,
                 f"{status} {location} {body[:300]}"):
        return None
    status, location, body = call(client, location)
    if not check("authorization answers the demo client with a code", redirect(status) and "/demo/callback" in location,
                 f"{status} {location} {body[:300]}"):
        return None
    status, _, body = call(client, location)
    page = Page(body)
    if not check("demo client page shows the code and its verifier",
                 status == 200 and page.text.get("code") and page.text.get("code_verifier"), f"{status} {body[:300]}"):
        return None
    return page


def redeem(page):
    status, _, body = call(plain, "/connect/token", {
        "grant_type": "authorization_code", "code": page.text["code"].strip(),
        "redirect_uri": issuer + "/demo/callback", "code_verifier": page.text["code_verifier"].strip(),
        "client_id": "test_client", "client_secret": "secret"})
    tokens = json.loads(body) if status == 200 else {}
    check("code exchange issues ID, access and refresh tokens",
          all(name in tokens for name in ("id_token", "access_token", "refresh_token")), f"{status} {body[:300]}")
    return tokens


user = browser()
callback = sign_in(user)
if callback:
    tokens = redeem(callback)
    if tokens:
        status, _, body = call(plain, "/connect/userinfo", headers={"Authorization": "Bearer " + tokens["access_token"]})
        check("userinfo names the signed-in user", status == 200 and json.loads(body).get("email") == login,
              f"{status} {body[:300]}")
        status, _, body = call(plain, "/connect/token", {"grant_type": "refresh_token", "refresh_token": tokens["refresh_token"],
                                                         "client_id": "test_client", "client_secret": "secret"})
        check("refresh token issues a new access token", status == 200 and "access_token" in json.loads(body),
              f"{status} {body[:300]}")

    # The demo client signs out with no ID token, so the provider asks the user to confirm first
    sign_out = callback.forms[-1]
    status, _, body = call(user, sign_out["action"], sign_out["fields"])
    confirmation = Page(body).forms
    if check("sign-out with no ID token asks to confirm", status == 200 and len(confirmation) == 1 and
             "confirmation" in confirmation[0]["fields"], f"{status} {body[:300]}"):
        status, location, body = call(user, confirmation[0]["action"], confirmation[0]["fields"])
        if check("confirmed sign-out returns to the demo client", redirect(status) and "/demo/signed-out" in location,
                 f"{status} {location} {body[:300]}"):
            status, _, body = call(user, location)
            check("demo client's signed-out page loads", status == 200, f"{status} {body[:200]}")
        status, location, _ = call(user, "/demo/start")
        status, location, _ = call(user, location)
        check("after sign-out the provider asks for the login page again", redirect(status) and "/Auth/Login" in location,
              f"{status} {location}")

# A sign-out with an ID token and no address to return to ends on the host's signed-out page
second = browser()
callback = sign_in(second)
if callback:
    tokens = redeem(callback)
    if tokens:
        status, location, body = call(second, "/connect/endsession", {"id_token_hint": tokens["id_token"]})
        check("sign-out with an ID token and no redirect shows a signed-out page",
              status == 200 and not Page(body).forms, f"{status} {location} {body[:300]}")

# The login page opened on its own: the sign-in ends on the home page
direct = browser()
status, _, body = call(direct, "/Auth/Login")
form = Page(body).forms[0]
status, location, body = call(direct, form["action"], dict(form["fields"], email=login, password=password))
check("sign-in with no authorization request goes home", redirect(status) and location == "/", f"{status} {location} {body[:200]}")

# A form another site submits carries no antiforgery token: the form again, and no session
forged = browser()
status, _, body = call(forged, "/Auth/Login", {"email": login, "password": password, "requestUri": ""})
check("login form without its antiforgery token is shown again", status == 200 and len(Page(body).forms) == 1,
      f"{status} {body[:300]}")
status, location, _ = call(forged, "/demo/start")
status, location, _ = call(forged, location)
check("that form signed nobody in", redirect(status) and "/Auth/Login" in location, f"{status} {location}")

# A form without credentials: the form again, not a server error
empty = browser()
status, _, body = call(empty, "/Auth/Login")
form = Page(body).forms[0]
status, _, body = call(empty, form["action"], dict(form["fields"]))
check("login form without credentials is shown again", status == 200 and len(Page(body).forms) == 1,
      f"{status} {body[:300]}")

print("FAILED: " + "; ".join(failures) if failures else "every flow passed")
sys.exit(1 if failures else 0)
