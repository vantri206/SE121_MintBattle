using UnityEngine;
using System;

public class SkillProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 15.0f;
    [SerializeField] private Vector3 offsetHitPos = new Vector3(0f, 1f, 0f);
    private Action onHitCallback;
    private bool hasHit = false;
    private Vector3 targetPos;

    public void Setup(Transform targetTransform, Action onHit)
    {
        this.targetPos = targetTransform.position + offsetHitPos;
        this.onHitCallback = onHit;
        Vector3 direction = targetPos - transform.position;

        if (direction.x < 0)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1f, transform.localScale.y, transform.localScale.z);
        }
        else
        {
            transform.localScale = transform.localScale;
        }
    }

    void Update()
    {
        if (targetPos == null)
        {
            onHitCallback?.Invoke();
            Destroy(gameObject);
            return;
        }

        if (hasHit) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            hasHit = true;
            onHitCallback?.Invoke(); 
            Destroy(gameObject);
        }
    }
}