import os
import random
import unittest
import requests
import dotenv
import jwt

class TestApiAuth(unittest.TestCase):
    def setUp(self):
        dotenv.load_dotenv()
        self.url = os.getenv("API_URL") + "/api/auth"
        self.rand_int = str(random.randint(0, 10**9)) # in case you want to run the test without relaunching the entire project
        self.claims_names = (
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        )


    def test_signup(self):
        credentials = {
            "username": "myuser" + self.rand_int,
            "password": "mypassword"
        }
        response = requests.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_signup_with_name(self):
        credentials = {
            "username": "myuser2" + self.rand_int,
            "password": "mypassword",
            "name": "My User"
        }
        response = requests.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_cannot_add_same_username(self):
        credentials = {
            "username": "user" + self.rand_int,
            "password": "password"
        }
        response = requests.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        credentials["password"] = "this is another user with the same username"
        response = requests.post(self.url + "/signup", json=credentials)
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)


    def test_login(self):
        credentials = {
            "username": "myuser" + self.rand_int,
            "password": "myuser",
        }
        response = requests.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode()
        self.assertIsNotNone(encoded_jwt)
    

    def test_change_password(self):
        credentials = {
            "username": "myuser" + self.rand_int,
            "password": "mypassword"
        }
        response = requests.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response = requests.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
        self.assertIsInstance(decoded_jwt, dict)
        for claim in self.claims_names:
            self.assertIsInstance(decoded_jwt[claim], str)
            if "sid" in claim:
                int(decoded_jwt[claim])



if __name__ == '__main__':
    unittest.main()
