import os
import random
import unittest
import requests
import dotenv
import jwt
import urllib3

class TestApiAuth(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        dotenv.load_dotenv()
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        cls.url = os.getenv("API_URL") + "/api/auth"
        cls.claims_names = (
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
        )
        cls.session = requests.Session()
        cls.session.verify = False


    # in case you want to rerun the test without reloading the project
    def rand_username(self): 
        return "myuser" + str(random.randint(0, 10**9))


    def test_signup_and_login(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)
        
        response = self.session.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode()
        self.assertIsNotNone(encoded_jwt)


    def test_signup_with_name(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword",
            "name": "My User"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_cannot_add_same_username(self):
        credentials = {
            "username": self.rand_username(),
            "password": "password"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        credentials["password"] = "this is another user with the same username"
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)


    def test_change_password(self):
        credentials = {
            "username": self.rand_username(),
            "password": "mypassword"
        }
        response = self.session.post(self.url + "/signup", json=credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response = self.session.post(self.url + "/login", json=credentials)
        encoded_jwt = response.content.decode().replace("\"", "")
        decoded_jwt = jwt.decode(encoded_jwt, algorithms=["HS256"], options={"verify_signature": False})
        self.assertIsInstance(decoded_jwt, dict)
        for claim in self.claims_names:
            self.assertIsInstance(decoded_jwt[claim], str)
            if "sid" in claim:
                int(decoded_jwt[claim])



if __name__ == '__main__':
    unittest.main()
