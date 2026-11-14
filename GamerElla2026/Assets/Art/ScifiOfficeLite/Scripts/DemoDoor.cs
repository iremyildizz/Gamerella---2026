using UnityEngine;

namespace ScifiOffice
{
    public class DemoDoor : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float openDistance = 1.5f;
        [SerializeField] private bool closeWhenPlayerLeaves = false;

        private Animator anim;
        private bool isOpen = false;

        private void Start()
        {
            anim = GetComponent<Animator>();

            if (player == null)
            {
                GameObject playerObj = GameObject.Find("Player");
                if (playerObj != null)
                {
                    player = playerObj.transform;
                }
            }
        }

        private void Update()
        {
            if (player == null || anim == null) return;

            // Measure horizontal (X/Z) distance so floor height differences don't skew the 1.5m check
            Vector3 doorPos = transform.position;
            Vector3 playerPos = player.position;
            doorPos.y = 0f;
            playerPos.y = 0f;

            float distance = Vector3.Distance(doorPos, playerPos);

            if (!isOpen && distance <= openDistance)
            {
                isOpen = true;
                anim.SetTrigger("Open");
            }
            else if (closeWhenPlayerLeaves && isOpen && distance > openDistance + 0.5f)
            {
                isOpen = false;
                anim.SetTrigger("Close");
            }
        }

        // Draws a cyan 1.5m radius circle around the door in the Scene view when selected
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, openDistance);
        }
    }
}