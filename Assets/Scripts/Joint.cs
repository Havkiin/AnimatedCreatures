using UnityEngine;

public class Joint : MonoBehaviour
{
    public Joint anchor;
    public Joint follower;

    public float distanceToAnchor;

    private void Start()
    {
        if (!anchor)
            return;

        anchor.follower = this;
        distanceToAnchor = Vector3.Distance(transform.position, anchor.transform.position);
    }

    public void UpdatePosition()
    {
        if (!anchor)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 anchorPosition = anchor.transform.position;

        Vector3 direction = (currentPosition - anchorPosition).normalized;
        transform.position = anchorPosition + direction * distanceToAnchor;
    }
}
