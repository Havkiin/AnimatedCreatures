using UnityEngine;
using UnityEngine.Rendering;

public class MovementComponent : MonoBehaviour
{
    public float movementSpeed = 1.0f;
    public float turnAngle = 20.0f;
    public float jointFlexibility = 45.0f;
    public float sensorAngle = 45.0f;

    public float wiggleFrequency = 4.0f;
    public float wiggleAmplitude = 8.0f;

    public Joint head;

    private bool hasArrived;
    private Vector3 currentDirection;
    private Vector3 currentDestination;

    private void Start()
    {
        currentDirection = Vector3.zero;
        currentDestination = Vector3.zero;
        hasArrived = true;
    }

    private void Update()
    {
        Wander();

        //if (hasArrived)
        //{
        //    currentDestination = PickDestination();
        //    hasArrived = false;
        //}

        //Seek(currentDestination);
    }

    void Wander()
    {
        Vector3 headPosition = head.transform.position;

        currentDirection = headPosition - head.follower.transform.position;
        currentDirection.Normalize();

        float wanderAngle = Random.Range(-turnAngle, turnAngle);

        // Check our sensors for incoming collisions
        int layerMask = 1 << 6;
        Vector3 leftSensor = Quaternion.Euler(0, 0, -sensorAngle) * currentDirection;
        Vector3 rightSensor = Quaternion.Euler(0, 0, sensorAngle) * currentDirection;

        bool hitLeft = Physics.Raycast(headPosition, leftSensor, 1.5f, layerMask);
        bool hitRight = Physics.Raycast(headPosition, rightSensor, 1.5f, layerMask);

        if (hitLeft)
        {
            wanderAngle += 45.0f;
        }
        else if (hitRight)
        {
            wanderAngle = -45.0f;
        }

        // Fish-like tail movement
        wanderAngle += wiggleAmplitude * Mathf.Sin(Time.time * wiggleFrequency);

        currentDirection = (Quaternion.Euler(0, 0, wanderAngle) * currentDirection) + currentDirection;
        currentDirection.Normalize();

        Vector3 newPosition = headPosition;
        newPosition += currentDirection * movementSpeed * Time.deltaTime;
        head.transform.position = newPosition;
    }

    void Seek(Vector3 destination)
    {
        if (!hasArrived)
        {
            currentDirection = destination - head.transform.position;
            currentDirection.Normalize();

            Vector3 newPosition = head.transform.position;
            newPosition += currentDirection * movementSpeed;
            head.transform.position = newPosition;
        }
        

        if (Vector3.Distance(head.transform.position, destination) < 1.0f)
        {
            hasArrived = true;
        }
    }

    Vector3 PickDestination()
    {
        float randX = Random.Range(-9.0f, 9.0f);
        float randY = Random.Range(-5.0f, 5.0f);

        return new Vector3 (randX, randY, 0);
    }
}
