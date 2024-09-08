import os
import pprint
import random
import unittest
from urllib import request
import dotenv
import requests
import jwt
import json
import urllib3
from signup_login import *

class TestApiUsers(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        dotenv.load_dotenv()
        urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
        cls.session = requests.Session()
        cls.session.verify = False
        cls.url: str = os.getenv("API_URL") 

        cls.jwt1, cls.user1 = signup_and_login(cls.url)
        cls.jwt2, cls.user2 = signup_and_login(cls.url)
        cls.jwt3, cls.user3 = signup_and_login(cls.url)
        cls.admin_jwt, cls.admin = login(cls.url, "admin", "admin")

        cls.url += "/api/users"


    def test_cannot_access_unauthenticated(self):
        response = self.session.get(self.url + "/" + self.user1["sid"])
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)
        

    def test_get_profile(self):
        response = self.session.get(self.url + "/" + self.user1["sid"], 
            headers={"Authorization": "Bearer " + self.jwt1},
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        expected_body = {
            "id": int(self.user1["sid"]),
            "username": self.user1["nameidentifier"],
            "name": self.user1["name"],
            "role": "user"
        }
        actual_body = json.loads(response.content.decode())
        actual_body = {key.lower(): value for key, value in actual_body.items()}
        self.assertEqual(len(expected_body), len(actual_body))


        for key in actual_body.keys():
            self.assertIn(key, expected_body)
        self.assertEqual(expected_body, actual_body)


    def test_cannot_access_other_user(self):
        response = self.session.get(self.url + "/" + self.user2["sid"],
            headers={"Authorization": "Bearer " + self.jwt1},
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_cannot_access_all_without_admin(self):
        response = self.session.get(self.url, headers={"Authorization": "Bearer " + self.jwt1})
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_access_all_with_admin(self):
        response = self.session.get(self.url, headers={"Authorization": "Bearer " + self.admin_jwt})
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        self.assertIsInstance(body, list)
        for user in body:
            self.assertIsInstance(user, dict)


    def test_access_other_user_with_admin(self):
        response = self.session.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        self.assertIsInstance(body, dict)


    def test_change_username(self):
        jwt, user = signup_and_login(os.getenv("API_URL"))
        new_username = "myupdateduser" + str(random.randint(0, 10**9))

        response = self.session.put(self.url + "/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt},
            json={
                "id": user["sid"],
                "username": new_username,
                "role": "user",
                "name": "mynewuser"
            }
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        # make sure the database was updated
        response = self.session.get(self.url + "/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        body = { key.lower(): value for key, value in body.items() }

        self.assertEqual(body["username"], new_username)


    def test_change_username_already_exists(self):
        new_username = self.user2["nameidentifier"]
        
        response = self.session.put(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1},
            json={
                "id": self.user1["sid"],
                "username": new_username,
                "role": "user",
                "name": "mynewuser",
            }
        )

        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_change_another_username(self):
        new_username = "someusername"
        
        response = self.session.put(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt2}, # unauthorized user
            json={
                "id": self.user1["sid"],
                "username": new_username,
                "role": "user",
                "name": "mynewuser",
            }
        )

        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_change_role_without_admin(self):
        response = self.session.put(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1}, 
            json={
                "id": self.user1["sid"],
                "username": self.user1["nameidentifier"],
                "role": "admin", # escalating own priviliges as a user
                "name": self.user1["name"],
            }
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # check to make sure nothing was changed
        response = self.session.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1}
        )
        body = json.loads(response.content.decode())
        body = { key.lower(): value for key, value in body.items() }
        self.assertTrue(body["role"], "user")


    def test_change_role_with_admin(self):
        response = self.session.put(self.url + "/" + self.user3["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt},
            json={
                "id": self.user3["sid"],
                "username": self.user3["nameidentifier"],
                "role": "admin",
                "name": self.user1["name"],
            }
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        # check to make sure user is now admin
        response = self.session.get(self.url + "/" + self.user3["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        body = json.loads(response.content.decode())
        body = { key.lower(): value for key, value in body.items() }

        self.assertIsInstance(body, dict)
        self.assertEqual(body["role"], "admin")


    def test_delete_user(self):
        url = os.getenv("API_URL") 
        jwt, user = signup_and_login(url)

        response = self.session.delete(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        # make sure the user cant login any more
        response = self.session.post(url + "/api/auth/login", json={
            "username": user["nameidentifier"],
            "password": "mypassword",
        })
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure that the user cannot access the DB anymore
        response = self.session.get(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_admin_delete_user(self):
        url = os.getenv("API_URL")
        jwt, user = signup_and_login(url)
        
        response = self.session.delete(self.url + "/" + user["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        
        # make sure the user cant login any more
        response = self.session.post(url + "/api/auth/login", json={
            "username": user["nameidentifier"],
            "password": "mypassword",
        })
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure that the user cannot access the DB anymore
        response = self.session.get(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_delete_other_user(self):
        response = self.session.delete(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt2} # unauthorized user
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure the user can still login
        response = self.session.post(os.getenv("API_URL") + "/api/auth/login",
            json={"username": self.user1["nameidentifier"], "password": "mypassword"}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        new_jwt = response.content.decode().replace("\"", "")
        self.assertGreater(len(new_jwt), 0)

        # make sure the user can still access profile
        response = self.session.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization" : "Bearer " + new_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        
        body = json.loads(response.content.decode())
        self.assertIsInstance(body, dict)
        body = {key.lower(): value for key, value in body.items()}

        self.assertEqual(body["id"], int(self.user1["sid"]))
        self.assertEqual(body["username"], self.user1["nameidentifier"])
        self.assertEqual(body["role"], self.user1["role"])
    


if __name__ == '__main__':
    unittest.main()
