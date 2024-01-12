using System.Collections;

using TMPro;

using UnityEngine;

using Vuforia;

public class DialogueManager : MonoBehaviour
{
	[Header("Timing")]
	[SerializeField, Range(1, 10), Tooltip("Seconds every line is in screen.")]
	float textSeconds = 5.0f;
	[SerializeField, Range(0, 2), Tooltip("Seconds between two lines.")]
	float betweenTextSeconds = 1.0f;
	[SerializeField, Range(0, 10), Tooltip("Seconds for Leshy to begin speaking.")]
	float startTextSeconds = 8.0f;
	[SerializeField, Range(10, 60), 
		Tooltip("Leshy will remind the player the last line every this amound of seconds.")]
	float remindSeconds = 15.0f;
	
	[Header("Dialogue")]
	[SerializeField]
    string[] dialogueWelcome, dialogueCard, dialogueBoard;

	[Header("Voice clips")]
	[SerializeField]
	AudioClip[] calmVoice, curiousVoice, frustratedVoice, laughingVoice;

	[Header("References")]
	[SerializeField]
	ImageTargetBehaviour boardTarget;
	[SerializeField]
	GameObject arrow;

	public bool waiting { get; set; } = false;

	char[] delimiters;
	public enum DialoguePhase { WELCOME, CARD, BOARD, BATTLE };
    public DialoguePhase dialoguePhase { get; set; }

	TextMeshProUGUI dialogueText;
	AudioSource audioSource;
	GameManager gameManager;
	static DialogueManager instance;

	private void Awake() {
		if (!instance)
			instance = this;
		else
			Destroy(this);
	}

	private void Start() {
		gameManager = GameManager.GetInstance();
		dialogueText = GetComponent<TextMeshProUGUI>();
		audioSource = GetComponent<AudioSource>();

		Hide();
		
		dialoguePhase = DialoguePhase.WELCOME;

		delimiters = new char[4];
		for (int i = 0; i < delimiters.Length; i++)
			delimiters[i] = char.Parse(i.ToString());

		StartCoroutine(WelcomeDialogue());
	}

	private void Update() {
		if (dialoguePhase == DialoguePhase.CARD && waiting && boardTarget.TargetStatus.Status != Status.NO_POSE) {
			dialoguePhase = DialoguePhase.BOARD;
			StartCoroutine(BoardDialogue());
		}
	}

	IEnumerator WelcomeDialogue() {
		yield return new WaitForSeconds(startTextSeconds);

		foreach(string line in dialogueWelcome) {
			yield return Say(line);
			yield return new WaitForSeconds(betweenTextSeconds);
		}

		waiting = true;

		yield return new WaitForSeconds(remindSeconds);
		while (dialoguePhase == DialoguePhase.WELCOME) {
			yield return Say(dialogueWelcome[dialogueWelcome.Length - 1]);
			yield return new WaitForSeconds(remindSeconds);
		}
	}

	public IEnumerator CardDialogue(string cardName) {
		dialoguePhase = DialoguePhase.CARD;
		
		yield return Say(dialogueCard[0] + cardName + "</color>!");
		yield return new WaitForSeconds(betweenTextSeconds);

		for (int i = 1; i < dialogueCard.Length; i++) {
			yield return Say(dialogueCard[i]);
			yield return new WaitForSeconds(betweenTextSeconds);
		}

		waiting = true;

		yield return new WaitForSeconds(remindSeconds);
		while (dialoguePhase == DialoguePhase.CARD) {
			yield return Say(dialogueCard[dialogueCard.Length - 1]);
			yield return new WaitForSeconds(remindSeconds);
		}
	}

	public IEnumerator BoardDialogue() {
		foreach (string line in dialogueBoard) {
			yield return Say(line);
			yield return new WaitForSeconds(betweenTextSeconds);
		}

		dialoguePhase = DialoguePhase.BATTLE;
		gameManager.BeginGame();
	}

	public IEnumerator Say(string text) {
		dialogueText.enabled = true;
		dialogueText.text = text.Split(delimiters, 2)[1];

		audioSource.clip = RandomClip(text);
		audioSource.Play();

		arrow.SetActive(true);

		yield return new WaitForSeconds(textSeconds);

		Hide();
	}

	void Hide() {
		dialogueText.enabled = false;
		arrow.SetActive(false);
	}

	AudioClip RandomClip(string text) => text[0] switch {
		'1' => curiousVoice[Random.Range(0, curiousVoice.Length)],
		'2' => frustratedVoice[Random.Range(0, frustratedVoice.Length)],
		'3' => laughingVoice[Random.Range(0, laughingVoice.Length)],
		_ => calmVoice[Random.Range(0, calmVoice.Length)]
	};

	public static DialogueManager GetInstance() {
		return instance;
	}
}
