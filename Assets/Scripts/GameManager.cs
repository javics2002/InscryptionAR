using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField, Range(0, 20),
        Tooltip("A match is won when a side of the scale has this value more of teeth than the other")]
    int winTeeth = 5;

    [Header("Board")]
    [SerializeField, Tooltip("Board transform")]
    Transform board;
    [SerializeField, Min(1), Tooltip("Number of lanes of the board")]
    int lanes = 4;
    [SerializeField]
    Scale scale;
    [SerializeField]
    float gravity = -1;

    [Header("Cards")]
    [SerializeField, Range(0, 2), Tooltip("Seconds a card takes to attack.")]
    float cardAttackSeconds = .4f;
	[SerializeField, Range(0, 2), Tooltip("Seconds a card takes to advance.")]
	float cardAdvanceSeconds = .4f;
	[SerializeField, Range(0, 2), Tooltip("Seconds between turns.")]
	float betweenTurnsSeconds = .8f;

	[Tooltip("Card slots are stored by lanes and for each row:\r\n" +
        "        0 is the player's row\r\n" +
        "        1 is the AI's row\r\n" +
        "        2 is the AI's queue row")]
    CardSlot[,] cardSlots;

    [SerializeField, Tooltip("Audio that plays when a card attacks.")]
    AudioClip cardAttackSound;
	[SerializeField, Tooltip("Audio that plays when a card receives an attack.")]
	AudioClip cardReceiveAttackSound;
	[SerializeField, Tooltip("Audio that plays when direct damage is dealt.")]
	AudioClip cardDirectAttackSound;
	[Tooltip("Audio that plays when the card dies.")]
	public AudioClip cardDeathSound;
	[SerializeField, Tooltip("Sounds of cards that will play when a card is played.")]
    AudioClip[] cardRandomSounds;

    [Header("AI")]
    [SerializeField, Tooltip("Cards the AI can spawn")]
    GameObject[] cardPrefabs;
    [SerializeField, Range(0, 4), Tooltip("AI starts the match with this number of cards already in the board.")]
    int startingCards;
    [SerializeField, Range(0, 4), Tooltip("AI starts the match with this number of cards already in queue.")]
    int startingQueuedCards;
    [SerializeField, Range(0, 10), Tooltip("Number of cards the AI can spawn until they stop spawning.")]
    int aiMaxHand;
	[SerializeField, Range(0, 4), Tooltip("Number of cards the AI can spawn until they stop spawning.")]
	float aiSpawnRate = 1.5f;
	float aiHand;

    [Header("Music")]
    [SerializeField] 
    AudioSource[] music;

	int[] damage = new int[2];
	int[] bones = new int[2];

	public List<Card> cards { get; private set; } = new List<Card>();

    Vector3 cardRotation = new Vector3(90, -90, 90);
	Vector3 cardScale = new Vector3(1.45f, 1.45f, 1.45f);

    enum Row { PLAYER, AI, AI_QUEUE }
    enum Turn { PLAYER, AI, NOT_PLAYING, TUTORIAL }
    Turn turn;

    DialogueManager dialogueManager;
    static GameManager instance;

    private void Awake()
    {
        if (!instance)
            instance = this;
        else
            Destroy(this);
	}

	private void Start() {
        dialogueManager = DialogueManager.GetInstance();

        turn = Turn.TUTORIAL;

		cardSlots = new CardSlot[3, lanes];
		for (int lane = 0; lane < lanes; lane++)
			for (int row = 0; row < 3; row++) {
				cardSlots[row, lane] = board.GetChild(row * lanes + lane).GetComponent<CardSlot>();
                cardSlots[row, lane].player = row == (int) Row.PLAYER;
            }
	}

    void Update() {
        Physics.gravity = gravity * board.up;
    }

    public void BeginGame() {
        StartCoroutine(SpawnCards());

        foreach(AudioSource audioSource in music)
            audioSource.Play();
    }

    public void EndPlayerTurn() {
        if (turn == Turn.PLAYER) {
            turn = Turn.AI;
			StartCoroutine(playTurn());
        }
    }

	IEnumerator SpawnCards() {
        aiHand = aiMaxHand;
        for(int i = 0; i < 2; i++) {
            damage[i] = 0;
            bones[i] = 0;
        }

		yield return new WaitForSeconds(betweenTurnsSeconds);

		//Spawn cards in random lanes
		for (int i = 0; i < startingCards; i++) {
            //Impove by marking the board and taking the empty slots
            int lane;
            do {
                lane = Random.Range(0, lanes);
            } while (cardSlots[(int) Row.AI, lane].card);

			GameObject card = Instantiate(cardPrefabs[0],
                cardSlots[(int) Row.AI, lane].transform, false);

            card.transform.Rotate(cardRotation);
            card.transform.localScale = cardScale;
            card.transform.GetChild(5).Rotate(new Vector3(0, 180, 0));

            Card cardComponent = card.GetComponent<Card>();
            cardSlots[(int) Row.AI, lane].card = cardComponent;

			yield return new WaitForSeconds(cardComponent.spawnSeconds);
		}

		for (int i = 0; i < startingQueuedCards; i++) {
			int lane;
			do {
				lane = Random.Range(0, lanes);
			} while (cardSlots[(int) Row.AI_QUEUE, lane].card);

			GameObject card = Instantiate(cardPrefabs[Random.Range(0, cardPrefabs.Length)],
                   cardSlots[(int) Row.AI_QUEUE, lane].transform, false);

			card.transform.Rotate(cardRotation + new Vector3(180, 0, 0));
			card.transform.localScale = cardScale;
			card.transform.GetChild(5).Rotate(new Vector3(0, 180, 0));

			Card cardComponent = card.GetComponent<Card>();
			cardSlots[(int) Row.AI_QUEUE, lane].card = cardComponent;

			yield return new WaitForSeconds(cardComponent.spawnSeconds);
		}

		turn = Turn.PLAYER;
	}

    IEnumerator playTurn() {
        //Player's cards attack
        for (int lane = 0; lane < lanes; lane++) {
            if(Attack(cardSlots[(int) Row.PLAYER, lane], cardSlots[(int) Row.AI, lane]))
			    yield return new WaitForSeconds(cardAttackSeconds);
        }

        yield return new WaitForSeconds(betweenTurnsSeconds);

		//AI's queued cards enter the board if possible
		for (int lane = 0; lane < lanes; lane++) {
            if (cardSlots[(int) Row.AI_QUEUE, lane].card && !cardSlots[(int) Row.AI, lane].card) {
                StartCoroutine(cardSlots[(int) Row.AI_QUEUE, lane]
                    .Advance(cardSlots[(int) Row.AI, lane], cardAdvanceSeconds));

				yield return new WaitForSeconds(cardAdvanceSeconds);
			}
		}

		yield return new WaitForSeconds(betweenTurnsSeconds);

		//AI's cards attack
		for (int lane = 0; lane < lanes; lane++) {
			if(Attack(cardSlots[(int) Row.AI, lane], cardSlots[(int) Row.PLAYER, lane]))
			    yield return new WaitForSeconds(cardAttackSeconds);
		}

        //Tilt scale
        yield return StartCoroutine(scale.tiltToValue(damage[1] - damage[0]));

        if (damage[0] - damage[1] >= winTeeth) {
            StartCoroutine(dialogueManager.Say("0Hasta aquí has llegado."));
            turn = Turn.NOT_PLAYING;

            yield return new WaitForSeconds(5);
            Application.Quit();
        }
        else if (damage[1] - damage[0] >= winTeeth) {
			StartCoroutine(dialogueManager.Say("0Hoy no vas a morir."));
			turn = Turn.NOT_PLAYING;

			yield return new WaitForSeconds(5);
			Application.Quit();
		}
        else {
            turn = Turn.PLAYER;

            //Spawn cards
            if (aiHand < 1)
                yield break;

            int cardsToSpawn = Mathf.FloorToInt(aiHand) - Mathf.FloorToInt(aiHand - aiSpawnRate);
			for (int i = 0; i < cardsToSpawn; i++) {
                int j = 0;
				int lane;
				do {
					lane = Random.Range(0, lanes);

                    if (j++ > 10)
                        yield break;
				} while (cardSlots[(int) Row.AI_QUEUE, lane].card);

				GameObject card = Instantiate(cardPrefabs[Random.Range(0, cardPrefabs.Length)],
					   cardSlots[(int) Row.AI_QUEUE, lane].transform, false);

				card.transform.Rotate(cardRotation + new Vector3(180, 0, 0));
				card.transform.localScale = cardScale;
				card.transform.GetChild(5).Rotate(new Vector3(0, 180, 0));

				Card cardComponent = card.GetComponent<Card>();
				cardSlots[(int) Row.AI_QUEUE, lane].card = cardComponent;

				yield return new WaitForSeconds(cardComponent.spawnSeconds);
			}

			aiHand -= aiSpawnRate;
		}
	}

    public bool Attack(CardSlot attacker, CardSlot receiver) {
        if (!attacker.card)
            return false;
        
		AudioSource attackerAudioSource = attacker.card.GetComponent<AudioSource>();

		if (receiver.card) {
			AudioSource receiverAudioSource = receiver.card.GetComponent<AudioSource>();
            attackerAudioSource.clip = cardAttackSound;
            attackerAudioSource.Play();
			receiverAudioSource.clip = cardReceiveAttackSound;
            receiverAudioSource.Play();

			if (receiver.card.RecieveAttack(attacker.card.GetAttack(), cardAttackSeconds)) {
                if(attacker.player)
					StartCoroutine(dialogueManager.Say("2¡No! Mi <color=red>" + receiver.card.cardName + "</color>!"));

				receiver.card = null;
			}
		}
        else {
			//Direct damage
			damage[attacker.player ? 1 : 0] += attacker.card.GetAttack();

			attackerAudioSource.clip = cardDirectAttackSound;
			attackerAudioSource.Play();
		}

        return true;
	}

    public void ResumeMusic() {
        if(turn == Turn.PLAYER || turn == Turn.AI)
		    foreach (AudioSource audioSource in music)
			    audioSource.Play();
	}

	public void AddCard(Card card) {
        cards.Add(card);
    }

    public AudioClip GetRandomCardSound() { 
        return cardRandomSounds[Random.Range(0, cardRandomSounds.Length)];
    }

    public int GetWinTeeth() {
        return winTeeth;
    }

    public static GameManager GetInstance()
    {
        return instance;
    }
}
