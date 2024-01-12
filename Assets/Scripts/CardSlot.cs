using System.Collections;
using UnityEngine;

public class CardSlot : MonoBehaviour
{
	[SerializeField, Range(0, 100), 
		Tooltip("A card within the center of the slot and this value will be played in this slot")]
	float slotScreenSizeInPixels = 50;

	[HideInInspector, Tooltip("True if this slot sharches for real cards from the player, " +
		"false if this slots is for use of the AI.")]
	public bool player { get; set; }

	GameManager gameManager;
	Camera cameraComponent;
	[HideInInspector]
	public Card card { get; set; } = null;

	private void Start() {
		gameManager = GameManager.GetInstance();
		cameraComponent = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
	}

	private void Update() {
		if (!card && player && DialogueManager.GetInstance().dialoguePhase == DialogueManager.DialoguePhase.BATTLE)
			foreach(Card cardTarget in gameManager.cards)
				if(!cardTarget.played && IsInSlot(cardTarget)) {
					card = cardTarget;
					card.Play();
				}
	}

	public IEnumerator Advance(CardSlot nextCardSlot, float advanceSeconds) {
		float time = 0;
		while (time < advanceSeconds) {
			float progress = time / advanceSeconds;

			card.transform.position = Vector3.Lerp(transform.position, nextCardSlot.transform.position, progress);
			
			time += Time.deltaTime;
			yield return null;
		}

		card.transform.parent = nextCardSlot.transform;
		nextCardSlot.card = card;
		card = null;

		nextCardSlot.card.transform.localPosition = Vector3.zero;
	}

	bool IsInSlot(Card card) {
		Vector2 slotScreenPosition = cameraComponent.WorldToScreenPoint(transform.position);
		Vector2 cardScreenPositon = card.screenPosition;

		float distanceToSlot = Vector2.Distance(slotScreenPosition, cardScreenPositon);
		return distanceToSlot <= slotScreenSizeInPixels;
	}
}
