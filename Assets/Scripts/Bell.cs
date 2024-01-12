using UnityEngine;

public class Bell : MonoBehaviour
{
	[SerializeField, Range(0, 1), Tooltip("Seconds to ring the bell again.")] 
	float ringCadenceSeconds = 0.05f;
    Animator animator;

	private void Start() {
		animator = GetComponent<Animator>();
	}

	public void Ring() {
		animator.SetBool("Ring", true);
		Invoke("Idle", ringCadenceSeconds);
	}

	void Idle() {
		animator.SetBool("Ring", false);
	}
}
