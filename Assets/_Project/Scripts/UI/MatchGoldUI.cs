using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MatchGoldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TMP_Text goldText;

    [Header("Animation")]
    [SerializeField, Min(0.01f)]
    private float countAnimationDuration =
        0.25f;

    private PlayerMatchProgression
        progression;

    private Coroutine bindRoutine;
    private Coroutine countRoutine;

    private void OnEnable()
    {
        SetGoldText(0);

        bindRoutine =
            StartCoroutine(
                BindToLocalPlayer()
            );
    }

    private IEnumerator BindToLocalPlayer()
    {
        while (NetworkManager.Singleton == null ||
               NetworkManager.Singleton.LocalClient ==
               null ||
               NetworkManager.Singleton.LocalClient
                   .PlayerObject == null)
        {
            yield return null;
        }

        NetworkObject localPlayerObject =
            NetworkManager.Singleton
                .LocalClient
                .PlayerObject;

        progression =
            localPlayerObject.GetComponent<
                PlayerMatchProgression>();

        if (progression == null)
        {
            progression =
                localPlayerObject
                    .GetComponentInChildren<
                        PlayerMatchProgression>();
        }

        if (progression == null)
        {
            Debug.LogError(
                "[MatchGoldUI] Yerel oyuncuda " +
                "PlayerMatchProgression bulunamadı."
            );

            bindRoutine = null;
            yield break;
        }

        progression.GoldChanged -=
            HandleGoldChanged;

        progression.GoldChanged +=
            HandleGoldChanged;

        SetGoldText(
            progression.Gold
        );

        bindRoutine = null;
    }

    private void HandleGoldChanged(
        int previousGold,
        int newGold)
    {
        if (countRoutine != null)
        {
            StopCoroutine(
                countRoutine
            );
        }

        countRoutine =
            StartCoroutine(
                AnimateGoldCount(
                    previousGold,
                    newGold
                )
            );
    }

    private IEnumerator AnimateGoldCount(
        int startingGold,
        int targetGold)
    {
        float elapsed = 0f;

        while (elapsed <
               countAnimationDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    countAnimationDuration
                );

            int displayedGold =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startingGold,
                        targetGold,
                        progress
                    )
                );

            SetGoldText(
                displayedGold
            );

            yield return null;
        }

        SetGoldText(
            targetGold
        );

        countRoutine = null;
    }

    private void SetGoldText(
        int gold)
    {
        if (goldText == null)
            return;

        goldText.text =
            gold.ToString();
    }

    private void Unbind()
    {
        if (progression != null)
        {
            progression.GoldChanged -=
                HandleGoldChanged;

            progression = null;
        }
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(
                bindRoutine
            );

            bindRoutine = null;
        }

        if (countRoutine != null)
        {
            StopCoroutine(
                countRoutine
            );

            countRoutine = null;
        }

        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();
    }
}