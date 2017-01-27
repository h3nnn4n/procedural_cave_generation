using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	Rigidbody rigidBody;
	Vector3 velocity;

	void Start () {
		rigidBody = GetComponent<Rigidbody> ();

	}

	void Update () {
		velocity = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0, Input.GetAxisRaw ("Vertical")).normalized * 10;
	}

	void FixedUpdate() {
		rigidBody.MovePosition (rigidBody.position + velocity * Time.fixedDeltaTime);
	}
}
