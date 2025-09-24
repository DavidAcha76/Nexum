using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayer : NetworkBehaviour
{
    public float speed = 5f;

    public override void FixedUpdateNetwork()
    {
        // Control local simple (si provees input)
        if (HasInputAuthority)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0, v).normalized;
            transform.position += dir * speed * Runner.DeltaTime;
        }
    }
}
