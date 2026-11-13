using BreakingPizza.Controls;
using System;
using System.Collections;
using BreakingPizza.Core;
using BreakingPizza.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace BreakingPizza.Interactables
{
    // Máscara: agarrarla y ponérsela.
    [RequireComponent(typeof(Collider))]
    public class MaskEquip : MonoBehaviour, IInteractable
    {
        private enum MaskState
        {
            InWorld,
            MovingToHand,
            InHand,
            MovingToFace,
            OnFace
        }

        [Header("Paso 1: agarrar (mundo -> mano)")]
        [SerializeField] private Transform handsRig;
        [SerializeField] private Vector3 equippedLocalPosition = new Vector3(0.15f, -0.2f, 0.4f);
        [SerializeField] private Vector3 equippedLocalEulerAngles = Vector3.zero;
        [SerializeField] private float equipAnimationDuration = 0.4f;

        [Header("Paso 2: ponérsela (mano -> cara), tecla K")]
        [SerializeField] private Transform faceMountPoint;
        [SerializeField] private Vector3 faceLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 faceLocalEulerAngles = Vector3.zero;
        [SerializeField] private float faceAnimationDuration = 0.3f;

        [Header("Stage")]
        [Tooltip("Se dispara recién al terminar el paso 2 (cuando la máscara llega a la cara), no al agarrarla.")]
        [SerializeField] private GameStage stageOnEquip = GameStage.Twist;

        [Header("UI (paso 1: prompt en el mundo)")]
        [SerializeField] private string promptText = "Click para agarrar la mascara";

        [Header("UI (paso 2: prompt \"en la mano\", conectar a InteractionPromptUI)")]
        [SerializeField] private string inHandPromptText = "Presione K para ponerse la mascara";
        public UnityEvent<string> OnHandPromptShown = new UnityEvent<string>();
        public UnityEvent OnHandPromptHidden = new UnityEvent();

        private MaskState state = MaskState.InWorld;

        // Para que los brazos en primera persona sepan cuándo mostrar la
        // mano sosteniendo la máscara.
        public bool IsBeingGrabbed => state == MaskState.MovingToHand;
        public bool IsHeld => state == MaskState.InHand;

        public string GetPrompt() => promptText;

        public void Interact()
        {
            if (state != MaskState.InWorld) return;
            state = MaskState.MovingToHand;

            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;

            Transform rig = handsRig != null ? handsRig : GameObject.Find("HandsRig")?.transform;
            if (rig == null)
            {
                Debug.LogWarning($"{name}: no encontré el Hands Rig — asignalo en el Inspector. Se salteó la animación de agarrar.", this);
                state = MaskState.InHand;
                OnHandPromptShown.Invoke(inHandPromptText);
                return;
            }

            StartCoroutine(MoveToRoutine(rig, equippedLocalPosition, equippedLocalEulerAngles, equipAnimationDuration, () =>
            {
                state = MaskState.InHand;
                OnHandPromptShown.Invoke(inHandPromptText);
            }));
        }

        private void Update()
        {
            if (state != MaskState.InHand) return;
            if (BreakingPizza.UI.PauseMenu.IsPaused) return;

            // K, o Triángulo en el control.
            if (GameInput.MaskPressed)
            {
                PutOnFace();
            }
        }

        private void PutOnFace()
        {
            if (faceMountPoint == null)
            {
                Debug.LogWarning($"{name}: falta asignar Face Mount Point en el Inspector.", this);
                return;
            }

            state = MaskState.MovingToFace;
            OnHandPromptHidden.Invoke();

            StartCoroutine(MoveToRoutine(faceMountPoint, faceLocalPosition, faceLocalEulerAngles, faceAnimationDuration, () =>
            {
                state = MaskState.OnFace;
                TriggerStageChange();
            }));
        }

        private IEnumerator MoveToRoutine(Transform targetParent, Vector3 localPosition, Vector3 localEulerAngles, float duration, Action onComplete)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 targetPosition = targetParent.TransformPoint(localPosition);
                Quaternion targetRotation = targetParent.rotation * Quaternion.Euler(localEulerAngles);

                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, targetPosition, t),
                    Quaternion.Slerp(startRotation, targetRotation, t));

                yield return null;
            }

            transform.SetParent(targetParent, false);
            transform.localPosition = localPosition;
            transform.localEulerAngles = localEulerAngles;

            onComplete?.Invoke();
        }

        private void TriggerStageChange()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetStage(stageOnEquip);
            }
            else
            {
                Debug.LogWarning($"{name}: no se encontró GameManager.Instance.", this);
            }
        }
    }
}
