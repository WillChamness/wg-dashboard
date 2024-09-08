import json
import os
import string
import unittest
import dotenv
import requests
import jwt
import urllib3
from signup_login import *


class Test_test_api_peers_read(unittest.TestCase):
    @staticmethod
    def add_peer(session: requests.Session, url: str, jwt: str, user: dict[str, str]) -> int:
        KEY_LENGTH = 44
        public_key = "".join(random.choices(string.ascii_letters + string.digits, k=KEY_LENGTH-1)) + "="
        response = session.post(url, 
            json={"publickey": public_key, "ownerid": user["sid"]},
            headers={"Authorization": "Bearer " + jwt}
        )
        body = json.loads(response.content.decode())
        assert isinstance(body, dict)
        assert isinstance(body["id"], int)
        return body["id"]


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
        cls.peers1: list[int] = []
        cls.peers2: list[int] = []
        cls.admin_peers: list[int] = []

        for _ in range(5):
            peer1id = cls.add_peer(cls.session, cls.peers_url, cls.jwt1, cls.user1)
            peer2id = cls.add_peer(cls.session, cls.peers_url, cls.jwt2, cls.user2)

            cls.peers1.append(peer1id)
            cls.peers2.append(peer2id)



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


    def test_admin_read_peer(self):
        responses = [self.session.get(
            self.peers_url + f"/{peer_id}",
            headers={"Authorization": "Bearer " + self.admin_jwt}
        ) for peer_id in [*self.peers1, *self.peers2, *self.admin_peers]]

        for response in responses:
            self.assertTrue(200 <= response.status_code and response.status_code <= 299)
            body = json.loads(response.content.decode())
            self.assertIsInstance(body, dict)
        


if __name__ == '__main__':
    unittest.main()
