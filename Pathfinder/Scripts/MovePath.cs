using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PathfindingScript))]
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class MovePath : MonoBehaviour
{
    PathfindingScript pathfindingScript;
    List<Vector3> path = new List<Vector3>();
    Rigidbody rb;

    [Tooltip("How close should the entity move to the next node to verify")]
    public float closeToNode;
    public float moveSpeed;
    int currentNode;

    float turnSmoothVel;
    public float turnSpeed;

    public float chaseRange;

    Vector3 prevPos;

    private void Start()
    {
        pathfindingScript = GetComponent<PathfindingScript>();
        rb = GetComponent<Rigidbody>();

        pathfindingScript.onPathRecalculated += RetrievePath;
    }

    private void Update()
    {

    }

    private void FixedUpdate()
    {
        if (currentNode > 0 && path.Count > 0)
        {
            if (Vector3.Distance(transform.position, path[currentNode]) < closeToNode)
            {
                currentNode--;

            }

            Vector3 nodeFormatted = new Vector3(path[currentNode].x, transform.position.y, path[currentNode].z);
            Vector3 dir = nodeFormatted - transform.position;

            if (Vector3.Distance(transform.position, path[0]) < chaseRange)
            {
                dir = path[0] - transform.position;
            }

            MoveCharacter(moveSpeed, dir, rb);

            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVel, turnSpeed);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }


    private void MoveCharacter(float speed, Vector3 direction, Rigidbody rb)
    {
        rb.MovePosition(transform.position + (direction * speed * Time.deltaTime));
    }

    private void RetrievePath(List<Vector3> positions)
    {
        path = positions;
        currentNode = positions.Count - 1;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, closeToNode * 2);
        Gizmos.DrawWireSphere(transform.position, chaseRange * 2);
    }
}
