using UnityEngine;
using TMPro;
using System;
using Vuforia;
using System.Collections;

public class Card : MonoBehaviour {
	[SerializeField, Tooltip("Cards of the player will be marked. Cards of the AI will not be marked.")]
	bool player;

	[Header("Card Data")]
	[Tooltip("Name of the card.")]
	public string cardName;
	[SerializeField, Tooltip("3D Model of the card.")]
	GameObject cardModel;
	[Tooltip("Scale of the model")]
	Vector3 modelScale;
	[Range(0, 2), Tooltip("Seconds of the animation of spawning and fainting.")]
	public float spawnSeconds;
	[SerializeField, Range(0, 1), Tooltip("Pitch variation of the animal cry when fainting.")]
	float faintPitch = .5f;

	[SerializeField, Min(0), Tooltip("Attack points. " +
		"The card will subtract this amount of health to the opposing card every turn.")]
	int attack = 0;
	[SerializeField, Min(1), Tooltip("Starting health of the card. " +
		"When it reaches 0, the card dies and leaves the board.")]
	int baseHealth = 1;
	[Tooltip("Current health of the card.")]
	int currentHealth = 0;

	[Header("Numbers")]
	[SerializeField, Tooltip("Color of the floating numbers representing attack and health values.")]
	Color numberNormalColor = Color.black;
	[SerializeField, Tooltip("The health value turns to this color")]
	Color numberDamagedColor = Color.red;
	[SerializeField, Range(1, 2), Tooltip("Size of the health number when it decreases")]
	float healthDamagedSize = 1.2f;

	[Tooltip("If the player places this card in one of their playable slots, the card will play.")]
	public bool played { get; private set; } = false;
    bool dead = false;

    public Vector3 screenPosition { get; private set; }

    TextMeshPro attackText, healthText, nameText;
    GameManager gameManager;
    Transform cameraTransform;
    Camera cameraComponent;
    ImageTargetBehaviour imageTargetBehaviour;
	ParticleSystem particles;
	AudioSource cardSound, animalCry;
	DialogueManager dialogueManager;

	private void Start()
    {
		gameManager = GameManager.GetInstance();
		gameManager.AddCard(this);

		dialogueManager = DialogueManager.GetInstance();

		GameObject arCamera = GameObject.FindGameObjectWithTag("MainCamera");
		cameraTransform = arCamera.transform;
		cameraComponent = arCamera.GetComponent<Camera>();

		attackText = transform.GetChild(0).GetComponent<TextMeshPro>();
		healthText = transform.GetChild(1).GetComponent<TextMeshPro>();
		nameText = transform.GetChild(2).GetComponent<TextMeshPro>();

		imageTargetBehaviour = GetComponent<ImageTargetBehaviour>();
		particles = GetComponentInChildren<ParticleSystem>();
		cardSound = GetComponent<AudioSource>();
		animalCry = GetComponentInChildren<AudioSource>();

		screenPosition = Vector3.zero;

		modelScale = cardModel.transform.localScale;

		Reset();

		if(!player)
			Play();
	}

	private void Update() {
		if (dead)
            return;

        if (player && !played) {
			if(imageTargetBehaviour.TargetStatus.Status == Status.NO_POSE) {
				screenPosition = Vector3.positiveInfinity;

				return;
			}

			if (dialogueManager.dialoguePhase == DialogueManager.DialoguePhase.WELCOME &&
				dialogueManager.waiting) {
				StartCoroutine(dialogueManager.CardDialogue(cardName));

				return;
			}

			//Card detected
			screenPosition = cameraComponent.WorldToScreenPoint(transform.position);

			if (Application.isEditor && Input.GetKey(KeyCode.E))
				Play();

			return;
		}

		//Rotate numbers to the camera
		attackText.transform.LookAt(cameraTransform, cameraTransform.up);
		healthText.transform.LookAt(cameraTransform, cameraTransform.up);
	}

	public void Reset() {
		currentHealth = baseHealth;

		attackText.SetText(attack.ToString());
		healthText.SetText(currentHealth.ToString());
		nameText.SetText(cardName);
		attackText.color = healthText.color = nameText.color = numberNormalColor;

		SetAllChildrenActive(false);
	}

    public void Play() {
        StartCoroutine(Spawn());
    }

	public bool RecieveAttack(int damage, float enlargeHealthNumberSeconds)
    {
		if(damage == 0)
			return false;

        currentHealth -= damage;

        if(currentHealth <= 0) {
            currentHealth = 0;
            StartCoroutine(Faint());

			return true;
        }

        healthText.color = numberDamagedColor;
		healthText.SetText(currentHealth.ToString());
		StartCoroutine(EnlargeHealthNumber(enlargeHealthNumberSeconds));

		return false;
    }

	IEnumerator Spawn() {
        played = true;
        SetAllChildrenActive(true);

		cardSound.clip = gameManager.GetRandomCardSound();
		cardSound.Play();
		particles.Play();

        float time = 0;
        while (time < spawnSeconds) {
            float progress = time / spawnSeconds;

			//Model grows
			cardModel.transform.localScale = Vector3.Lerp(Vector3.zero, modelScale, progress);

			//Floating text appears
			numberNormalColor.a = progress;
			attackText.color = healthText.color = numberNormalColor;

			time += Time.deltaTime;
			yield return null;
        }

		animalCry.Play();
	}

	IEnumerator Faint() {
		cardSound.clip = gameManager.cardDeathSound;
		cardSound.Play();
		animalCry.pitch = faintPitch;
		animalCry.Play();

		float time = 0;
		while (time < spawnSeconds) {
			float progress = time / spawnSeconds;

			//Model shrinks
			cardModel.transform.localScale = Vector3.Lerp(modelScale, Vector3.zero, progress);

			//Floating text disappears
			numberNormalColor.a = numberDamagedColor.a = 1 - progress;
			attackText.color = numberNormalColor;
			healthText.color = numberDamagedColor;

			time += Time.deltaTime;
			yield return null;
		}

		SetAllChildrenActive(false);
		dead = true;
	}

	IEnumerator EnlargeHealthNumber(float seconds) {
		Vector3 normalScale = new Vector3(-1, 1, 1);
		Vector3 largeScale = normalScale * healthDamagedSize;

		float time = 0;
		while (time < spawnSeconds) {
			float progress = time / spawnSeconds;

			healthText.rectTransform.localScale = Vector3.Lerp(normalScale, largeScale, ParabolicTween(progress));

			time += Time.deltaTime;
			yield return null;
		}
	}

	public int GetAttack() {
		return attack;
	}

	void SetAllChildrenActive(bool active) {
		for (int i = 0; i < transform.childCount; i++)
			transform.GetChild(i).gameObject.SetActive(active);
	}

	float ParabolicTween(float t) {
		return -1.5f * Mathf.Pow(t, 2) + .5f * t + 1;
	}
}
