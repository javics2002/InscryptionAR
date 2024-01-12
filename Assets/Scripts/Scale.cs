using System.Collections;
using UnityEngine;

public class Scale : MonoBehaviour
{
	[Header("References")]
	[SerializeField, Tooltip("Prefab of the token that represents damage points in the scale")]
	GameObject tokenPrefab;
	[SerializeField]
	Transform articulation, leftPlate, rightPlate;
	[SerializeField]
	Transform arrow;
	[SerializeField, Tooltip("Transform where teeth for the player will spawn")]
	Transform playerTeethSpawn;
	[SerializeField, Tooltip("Transform where teeth for the AI will spawn")]
	Transform aiTeethSpawn;
	[SerializeField, Tooltip("One of these clips will be played when spawning a tooth.")]
	AudioClip[] teethSounds;

	[Header("Maximum rotation")]
	[SerializeField, Tooltip("Max balanced rotation of the arrow against the AI.")]
	Vector3 maxScaleRotation;
	[SerializeField, Tooltip("Max balanced rotation of the arrow against the AI.")] 
	Vector3 maxArrowRotation;

	[Header("Timing")]
	[SerializeField, Range(0, 1), Tooltip("Seconds each step separates from the previous one.")]
	float stepRateSeconds = .2f;
	[SerializeField, Range(0, 1), Tooltip("Seconds the actual step takes.")]
	float stepSeconds = .05f;

	int value, maxValue;
	Vector3 stepScaleRotation, stepArrowRotation;
	[Tooltip("Pitch that the audio clip has for each value.")]
	float[] pitch;
	AudioSource audioSource;
    GameManager gameManager;

	private void Start() {
		audioSource = GetComponent<AudioSource>();

		gameManager = GameManager.GetInstance();
		maxValue = gameManager.GetWinTeeth();
		value = 0;

		stepSeconds = Clamp(stepSeconds, 0, stepRateSeconds);
		stepScaleRotation = maxScaleRotation / maxValue;
		stepArrowRotation = maxArrowRotation / maxValue;

		int notes = maxValue + 7 - maxValue % 7;
		pitch = new float[notes];

		for(int i = 0; i < notes; i += 7) {
			pitch[i + 0] = Mathf.Pow(2, i / 12);
			pitch[i + 1] = Mathf.Pow(2, i + 2 / 12);
			pitch[i + 2] = Mathf.Pow(2, i + 4 / 12);
			pitch[i + 3] = Mathf.Pow(2, i + 5 / 12);
			pitch[i + 4] = Mathf.Pow(2, i + 7 / 12);
			pitch[i + 5] = Mathf.Pow(2, i + 9 / 12);
			pitch[i + 6] = Mathf.Pow(2, i + 11 / 12);
		}
	}

	public IEnumerator tiltToValue(int newValue) {
		newValue = Clamp(newValue, -maxValue, maxValue);

		if (value < newValue)
			for (int i = value; i < newValue; i++) {
				//Spawn teeth
				AudioSource toothAudioSource =
					Instantiate(tokenPrefab, playerTeethSpawn.position, Random.rotation).GetComponent<AudioSource>();
				toothAudioSource.clip = teethSounds[Random.Range(0, teethSounds.Length)];
				toothAudioSource.Play();

				yield return new WaitForSeconds(stepRateSeconds - stepSeconds);

				//Ticking
				float time = 0;
				while (time < stepSeconds) {
					float deltaProgress = Time.deltaTime / stepSeconds;

					articulation.Rotate(stepScaleRotation * deltaProgress);
					leftPlate.Rotate(-stepScaleRotation * deltaProgress);
					rightPlate.Rotate(-stepScaleRotation * deltaProgress);
					arrow.Rotate(stepArrowRotation * deltaProgress);

					time += Time.deltaTime;
					yield return null;
				}

				audioSource.pitch = pitch[Mathf.Abs(i)];
				audioSource.Play();
			}
		else
			for (int i = value; i > newValue; i--) {
				//Spawn teeth
				AudioSource toothAudioSource = 
					Instantiate(tokenPrefab, aiTeethSpawn.position, Random.rotation).GetComponent<AudioSource>();
				toothAudioSource.clip = teethSounds[Random.Range(0, teethSounds.Length)];
				toothAudioSource.Play();

				yield return new WaitForSeconds(stepRateSeconds - stepSeconds);

				//Ticking
				float time = 0;
				while (time < stepSeconds) {
					float deltaProgress = Time.deltaTime / stepSeconds;

					articulation.Rotate(-stepScaleRotation * deltaProgress);
					leftPlate.Rotate(stepScaleRotation * deltaProgress);
					rightPlate.Rotate(stepScaleRotation * deltaProgress);
					arrow.Rotate(-stepArrowRotation * deltaProgress);

					time += Time.deltaTime;
					yield return null;
				}

				audioSource.pitch = pitch[Mathf.Abs(i)];
				audioSource.Play();
			}

		value = newValue;

		articulation.localRotation = Quaternion.Euler(maxScaleRotation * value / maxValue);
		leftPlate.localRotation = Quaternion.Euler(-maxScaleRotation * value / maxValue);
		rightPlate.localRotation = Quaternion.Euler(-maxScaleRotation * value / maxValue);
		arrow.localRotation = Quaternion.Euler(maxArrowRotation * value / maxValue);
	}

	int Clamp(int value, int min, int max) {
		if(value < min)
			return min;

		if(value > max)
			return max;

		return value;
	}

	float Clamp(float value, float min, float max) {
		if (value < min)
			return min;

		if (value > max)
			return max;

		return value;
	}
}
