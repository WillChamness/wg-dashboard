import json
import os
import unittest
import dotenv
import requests
import jwt
import urllib3
from signup_login import *


class Test_test_api_peers_read(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        dotenv.load_dotenv()
        cls.base_url = os.getenv("API_URL")
        cls.peers_url = cls.base_url + "/api/peers"
        cls.session = requests.Session()
        cls.session.verify = False
        cls.jwt1, cls.user1 = signup_and_login(cls.base_url)
        cls.jwt2, cls.user2 = signup_and_login(cls.base_url)
        cls.admin_jwt, cls.admin = login(cls.base_url, "admin", "admin")


    def test_admin_get_all(self):
        response = self.session.get(self.peers_url, headers={"Authorization": "Bearer " + self.admin_jwt})
        self.assertTrue(200 <= response.status_code and response.status_code <= 299)
        
        body = json.loads(response.content.decode())
        self.assertIsInstance(body, list)
        for peer in body:
            self.assertIsInstance(peer, dict)
            

    def test_unauthorized_get_all(self):
        response = self.session.get(self.peers_url, headers={"Authorization": "Bearer " + self.jwt1})
        self.assertTrue(400 <= response.status_code and response.status_code <= 499)


if __name__ == '__main__':
    unittest.main()
