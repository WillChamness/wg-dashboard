import os
import random
import unittest
import dotenv
import requests
import jwt
import json

class TestApiUsers(unittest.TestCase):
    @staticmethod
    def _signup_and_login(url: str) -> tuple[str, dict[str, str]]:
        randint = str(random.randint(0, 10**9)) # in case you want to run the test without relaunching the entire project
        response = requests.post(url + "/api/auth/signup", json={
            "username": "myuser" + randint,
            "password": "mypassword",
            "name": "Test User"
        })
        assert 200 <= response.status_code and response.status_code <= 299
        response = requests.post(url + "/api/auth/login", json={
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
        )
        claims = dict()
        for claim_name in claims_names:
            claim = claim_name.split("/")[-1]
            claims[claim] = decoded_jwt[claim_name]

        return (encoded_jwt, claims)

        
    def setUp(self):
        dotenv.load_dotenv()
        self.url: str = os.getenv("API_URL") 
        self.jwt1, self.user1 = self._signup_and_login(self.url)
        self.jwt2, self.user2 = self._signup_and_login(self.url)
        self.url += "/api/users"


    def test_cannot_access_unauthenticated(self):
        STATUS_CODE_UNAUTHENTICATED = 401
        response = requests.get(self.url + "/" + self.user1["sid"])
        self.assertEqual(response.status_code, STATUS_CODE_UNAUTHENTICATED)
        

    def test_get_profile(self):
        response = requests.get(self.url + "/" + self.user1["sid"], 
            headers={"Authorization": "Bearer " + self.jwt1},
        )
        expected_body = {
            "id": int(self.user1["sid"]),
            "username": self.user1["nameidentifier"],
            "name": self.user1["name"],
            "role": "user"
        }
        actual_body = json.loads(response.content.decode())
        self.assertEqual(len(expected_body), len(actual_body))

        for key in actual_body.keys():
            self.assertIn(key.lower(), expected_body)
        for key in actual_body.keys():
            self.assertEqual(expected_body[key.lower()], actual_body[key])


    def test_cannot_access_other_user(self):
        STATUS_CODE_NOT_FOUND = 404 # api returns not found if unauthorized
        response = requests.get(self.url + "/" + self.user2["sid"],
            headers={"Authorization": "Bearer " + self.jwt1},
        )
        self.assertEqual(response.status_code, STATUS_CODE_NOT_FOUND)

    

if __name__ == '__main__':
    unittest.main()
