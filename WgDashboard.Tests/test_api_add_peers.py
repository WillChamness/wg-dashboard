import os
import pprint
import random
import unittest
import dotenv
import string
import requests
import jwt

class test_api_add_peers(unittest.TestCase):
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
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        )
        claims = dict()
        for claim_name in claims_names:
            claim = claim_name.split("/")[-1]
            claims[claim] = decoded_jwt[claim_name]

        return (encoded_jwt, claims)


    @staticmethod
    def _login(url: str, username: str, password: str) -> tuple[str, dict[str, str]]:
        assert len(username) > 0
        assert len(password) > 0
        response = requests.post(url + "/api/auth/login", json={
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


    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        dotenv.load_dotenv()
        self.base_url = os.getenv("API_URL")
        self.jwt1, self.user1 = self._signup_and_login(self.base_url)
        self.jwt2, self.user2 = self._signup_and_login(self.base_url)
        self.admin_jwt, self.admin = self._login(self.base_url, "admin", "admin")
        self.KEY_LENGTH = 44


    def test_add_keys(self):
        public_key1 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key2 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        public_key3 = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        allowed_ips = "127.0.0.1/32"
        owner_id = self.user1["sid"]
        reqs = [
            {"publickey": public_key1, "allowedips": allowed_ips, "ownerid": owner_id},
            {"publickey": public_key2, "allowedips": allowed_ips, "ownerid": owner_id},
            {"publickey": public_key3, "allowedips": allowed_ips, "ownerid": owner_id},
        ]

        responses = [requests.post(
            self.base_url + "/api/peers/",
            headers={"Authorization": "Bearer " + self.jwt1},
            json=body
        ) for body in reqs]

        for response in responses:
            self.assertGreaterEqual(response.status_code, 200)
            self.assertLessEqual(response.status_code, 299)


    def test_key_already_exists(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="

        response = requests.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt1},
            json={"publickey": public_key, "allowedips": "127.0.0.1/32", "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        response = requests.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2},
            json={"public_key": public_key, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_add_key_to_other_user(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        
        response = requests.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2}, # unauthorized user
            json={"public_key": public_key, "allowedips": "127.0.0.1/32", "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)
        

    def test_add_key_as_admin(self):
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=self.KEY_LENGTH-1)) + "="
        
        response = requests.post(self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.admin_jwt},
            json={"public_key": public_key, "allowedips": "127.0.0.1/32", "ownerid": self.user1["sid"]}
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
            {"publickey": public_key1, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]},
            {"publickey": public_key2, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]},
            {"publickey": public_key3, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]},
            {"publickey": public_key4, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]},
            {"publickey": public_key5, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]},
        ]

        responses = [
            requests.post(
                self.base_url + "/api/peers",
                headers={"Authorization": "Bearer " + self.jwt2},
                json=body
            )
            for body in reqs
        ]

        for response in responses:
            self.assertGreaterEqual(response.status_code, 200)
            self.assertLessEqual(response.status_code, 299)

        response = requests.post(
            self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt2},
            json={"publickey": public_key6, "allowedips": "127.0.0.1/32", "ownerid": self.user2["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_add_bad_publickey(self):
        public_key = "thisshouldntbeallowed"
        response = requests.post(
            self.base_url + "/api/peers",
            headers={"Authorization": "Bearer " + self.jwt1},
            json={"publickey": public_key, "allowedips": "127.0.0.1/32", "ownerid": self.user1["sid"]}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)



        
if __name__ == '__main__':
    unittest.main()
