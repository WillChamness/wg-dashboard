import requests
import jwt
import random

def signup_and_login(url: str) -> tuple[str, dict[str, str]]:
    randint = str(random.randint(0, 10**9)) # in case you want to run the test without relaunching the entire project
    response = requests.post(url + "/api/auth/signup", verify=False, json={
        "username": "myuser" + randint,
        "password": "mypassword",
        "name": "Test User"
    })
    assert 200 <= response.status_code and response.status_code <= 299
    response = requests.post(url + "/api/auth/login", verify=False, json={
        "username": "myuser" + randint,
        "password": "mypassword"
    })
    assert 200 <= response.status_code and response.status_code <= 299

    encoded_jwt = response.content.decode().replace("\"", "")
    decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
    claims_names = (
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    )
    claims = dict()
    for claim_name in claims_names:
        claim = claim_name.split("/")[-1]
        claims[claim] = decoded_jwt[claim_name]

    return (encoded_jwt, claims)



def login(url: str, username: str, password: str) -> tuple[str, dict[str, str]]:
    assert len(username) > 0
    assert len(password) > 0
    response = requests.post(url + "/api/auth/login", verify=False, json={
        "username": username,
        "password": password
    })
    assert 200 <= response.status_code and response.status_code <= 299

    encoded_jwt = response.content.decode().replace("\"", "")
    decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
    claims_names = (
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    )
    claims = dict()
    for claim_name in claims_names:
        claim = claim_name.split("/")[-1]
        claims[claim] = decoded_jwt[claim_name]

    return (encoded_jwt, claims)
