import os
import pprint
import random
import unittest
import dotenv
import string
import requests
import jwt
import urllib3
from signup_login import *

class test_api_add_peers(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        dotenv.load_dotenv()
        cls.base_url = os.getenv("API_URL")
        cls.session = requests.Session()
        cls.session.verify = False
        cls.jwt1, cls.user1 = signup_and_login(cls.base_url)
        cls.jwt2, cls.user2 = signup_and_login(cls.base_url)
        cls.admin_jwt, cls.admin = login(cls.base_url, "admin", "admin")
        cls.KEY_LENGTH = 44


    def test_add_keys(self):
        public_key1 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key2 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key3 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        owner_id = self.user1["sid"]
        reqs = [
            {"publickey": public_key1, "ownerid": owner_id},
            {"publickey": public_key2, "ownerid": owner_id},
            {"publickey": public_key3, "ownerid": owner_id},
        ]

        responses = [self.session.post(
            self.base_url + "/api/peers/",
            headers={"Authorization": "Bearer " + self.jwt1},
            json=body
        ) for body in reqs]

        for response in responses:
            self.assertGreaterEqual(response.status_code, 200)
            self.assertLessEqual(response.status_code, 299)


    def test_key_already_exists(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="

        response = self.session.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt1},
            json={"publickey": public_key, "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        response = self.session.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2},
            json={"public_key": public_key, "ownerid": self.user2["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_add_key_to_other_user(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        
        response = self.session.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2}, # unauthorized user
            json={"public_key": public_key, "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)
        

    def test_add_key_as_admin(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        
        response = self.session.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.admin_jwt},
            json={"public_key": public_key, "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_add_too_many_keys(self):
        public_key1 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key2 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key3 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key4 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key5 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key6 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="

        reqs = [
            {"publickey": public_key1, "ownerid": self.user2["sid"]},
            {"publickey": public_key2, "ownerid": self.user2["sid"]},
            {"publickey": public_key3, "ownerid": self.user2["sid"]},
            {"publickey": public_key4, "ownerid": self.user2["sid"]},
            {"publickey": public_key5, "ownerid": self.user2["sid"]},
        ]

        responses = [
            self.session.post(
                self.base_url + "/api/peers",
                headers={"Authorization": "Bearer " + self.jwt2},
                json=body
            )
            for body in reqs
        ]

        for response in responses:
            self.assertGreaterEqual(response.status_code, 200)
            self.assertLessEqual(response.status_code, 299)

        response = self.session.post(
            self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2},
            json={"publickey": public_key6, "ownerid": self.user2["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_add_bad_publickey(self):
        public_key = "thisshouldntbeallowed"
        response = self.session.post(
            self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt1},
            json={"publickey": public_key, "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)



        
if __name__ == '__main__':
    unittest.main()
