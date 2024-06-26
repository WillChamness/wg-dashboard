import os
import pprint
import random
import unittest
from urllib import request
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

        
    def setUp(self):
        dotenv.load_dotenv()
        self.url: str = os.getenv("API_URL") 
        self.jwt1, self.user1 = self._signup_and_login(self.url)
        self.jwt2, self.user2 = self._signup_and_login(self.url)
        self.jwt3, self.user3 = self._signup_and_login(self.url)
        self.admin_jwt, self.admin = self._login(self.url, "admin", "admin")

        self.url += "/api/users"


    def test_cannot_access_unauthenticated(self):
        response = requests.get(self.url + "/" + self.user1["sid"])
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)
        

    def test_get_profile(self):
        response = requests.get(self.url + "/" + self.user1["sid"], 
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
        self.assertEqual(len(expected_body), len(actual_body))

        for key in actual_body.keys():
            self.assertIn(key.lower(), expected_body)
        for key in actual_body.keys():
            self.assertEqual(expected_body[key.lower()], actual_body[key])


    def test_cannot_access_other_user(self):
        response = requests.get(self.url + "/" + self.user2["sid"],
            headers={"Authorization": "Bearer " + self.jwt1},
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_cannot_access_all_without_admin(self):
        response = requests.get(self.url, headers={"Authorization": "Bearer " + self.jwt1})
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

    def test_access_all_with_admin(self):
        response = requests.get(self.url, headers={"Authorization": "Bearer " + self.admin_jwt})
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        self.assertIsInstance(body, list)
        for user in body:
            self.assertIsInstance(user, dict)


    def test_access_other_user_with_admin(self):
        response = requests.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        self.assertIsInstance(body, dict)


    def test_change_username(self):
        new_username = "myupdateduser" + str(random.randint(0, 10**9))

        response = requests.put(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1},
            json={
                "id": self.user1["sid"],
                "username": new_username,
                "role": "user",
                "name": "mynewuser"
            }
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        response = requests.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        body = json.loads(response.content.decode())
        body = { key.lower(): value for key, value in body.items() }

        self.assertEqual(body["username"], new_username)


    def test_change_username_already_exists(self):
        new_username = self.user2["nameidentifier"]
        
        response = requests.put(self.url + "/" + self.user1["sid"],
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
        
        response = requests.put(self.url + "/" + self.user1["sid"],
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
        response = requests.put(self.url + "/" + self.user1["sid"],
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
        response = requests.get(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt1}
        )
        body = json.loads(response.content.decode())
        body = { key.lower(): value for key, value in body.items() }
        self.assertTrue(body["role"], "user")


    def test_change_role_with_admin(self):
        response = requests.put(self.url + "/" + self.user3["sid"],
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
        response = requests.get(self.url + "/" + self.user3["sid"],
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
        jwt, user = self._signup_and_login(url)

        response = requests.delete(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)

        # make sure the user cant login any more
        response = requests.post(url + "/api/auth/login", json={
            "username": user["nameidentifier"],
            "password": "mypassword",
        })
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure that the user cannot access the DB anymore
        response = requests.get(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_admin_delete_user(self):
        url = os.getenv("API_URL")
        jwt, user = self._signup_and_login(url)
        
        response = requests.delete(self.url + "/" + user["sid"],
            headers={"Authorization": "Bearer " + self.admin_jwt}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        
        # make sure the user cant login any more
        response = requests.post(url + "/api/auth/login", json={
            "username": user["nameidentifier"],
            "password": "mypassword",
        })
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure that the user cannot access the DB anymore
        response = requests.get(url + "/api/users/" + user["sid"],
            headers={"Authorization": "Bearer " + jwt}
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)


    def test_delete_other_user(self):
        response = requests.delete(self.url + "/" + self.user1["sid"],
            headers={"Authorization": "Bearer " + self.jwt2} # unauthorized user
        )
        self.assertGreaterEqual(response.status_code, 400)
        self.assertLessEqual(response.status_code, 499)

        # make sure the user can still login
        response = requests.post(os.getenv("API_URL") + "/api/auth/login",
            json={"username": self.user1["nameidentifier"], "password": "mypassword"}
        )
        self.assertGreaterEqual(response.status_code, 200)
        self.assertLessEqual(response.status_code, 299)
        new_jwt = response.content.decode().replace("\"", "")
        self.assertGreater(len(new_jwt), 0)

        # make sure the user can still access profile
        response = requests.get(self.url + "/" + self.user1["sid"],
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
