import unittest
import os
import random
import unittest
import requests
import urllib3
import dotenv
import jwt
from signup_login import *

class TestApiRefresh(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        dotenv.load_dotenv()
        cls.base_url: str = os.getenv("API_URL") 
        cls.url = cls.base_url + "/api/auth"
        cls.session = requests.Session()
        cls.session.verify = False
        cls.jwt, cls.user = signup_and_login(cls.base_url)
        cls.credentials = {
            "username": cls.user["nameidentifier"],
            "password": "mypassword",
        }
        

    def test_refresh_request(self):
        response = self.session.post(self.url + "/login", json=self.credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response_cookies = response.cookies.get_dict()
        self.assertIn("RefreshToken", response_cookies)
        refresh_token = response_cookies["RefreshToken"]
        self.assertIsNotNone(refresh_token)
        self.assertIsInstance(refresh_token, str)


    def test_refresh_rotation(self):
        response = self.session.post(self.url + "/login", json=self.credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response_cookies = response.cookies.get_dict()
        refresh_token = response_cookies["RefreshToken"]

        response = self.session.post(self.url + "/refresh", cookies={"RefreshToken": refresh_token})
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response_cookies = response.cookies.get_dict()
        new_refresh_token = response_cookies["RefreshToken"]

        self.assertNotEqual(refresh_token, new_refresh_token)


    def test_new_jwt(self):
        response = self.session.post(self.url + "/login", json=self.credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)

        response_cookies = response.cookies.get_dict()
        refresh_token = response_cookies["RefreshToken"]

        new_jwt = response.content.decode().replace("\"", "")
        self.assertNotEqual(self.jwt, new_jwt)

        response = self.session.get(self.base_url + "/api/users/" + self.user["sid"],
            headers={"Authorization": "Bearer " + new_jwt}
        )
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)


    def test_cannot_use_old_refresh_token(self):
        response = self.session.post(self.url + "/login", json=self.credentials)
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)
        
        response_cookies = response.cookies.get_dict()
        old_refresh_token = response_cookies["RefreshToken"]
        response = self.session.post(self.url + "/refresh", cookies={"RefreshToken": old_refresh_token})
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)
        
        response = self.session.post(self.url + "/refresh", cookies={"RefreshToken": old_refresh_token})
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)



if __name__ == '__main__':
    unittest.main()
