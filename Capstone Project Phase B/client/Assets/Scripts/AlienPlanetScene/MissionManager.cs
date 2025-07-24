using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionManager : Singleton<MissionManager>, IUpdateObserver {
    // Hash for animator "Speed" parameter
    private static readonly int SpeedFloat = Animator.StringToHash("Speed");

    // Indicates if the mission has started
    public bool MissionStarted { get; private set; }

    // The index of the current checkpoint (-1 means not started)
    public int CurrentCheckpoint { get; private set; } = -1;

    // Total number of checkpoints defined in the mission
    public int TotalCheckpoints => missionData.checkpoints.Length;

    [SerializeField] private DialogueMissionData missionData; // Mission data containing dialogue and checkpoints
    [SerializeField] private Camera playerCamera; // Reference to the player's camera
    [SerializeField] private NavMeshAgent npcAgent; // NPC agent that moves along the checkpoints
    [SerializeField] private Animator npcAnimator; // Animator controlling NPC movement
    [SerializeField] private GameObject teacherNpc; // Reference to the teacher NPC
    [SerializeField] private Image fadeImage; // Fade image used at mission start
    [SerializeField] private TextMeshProUGUI missionText; // UI text for displaying mission-related messages
    [SerializeField] private VisualGuide visualGuide; // Visual guide that is activated at the end of the mission

    private Vector3 _targetPosition; // Current destination position for the NPC
    private Quaternion _originalCameraRotation; // Stores the camera rotation before focusing
    private Transform _currentLookTarget; // Current target the NPC or camera should look at
    private Animator _teacherAnimator; // Animator component of the teacher NPC
    private float _npcStoppingDistanceSqr; // Squared stopping distance for NPC
    private bool _isMovingToCheckpoint; // Whether the NPC is currently moving
    private bool _isWaitingForInteraction; // Whether waiting for player interaction
    private bool _isActionActive; // Whether a checkpoint action is currently active

    private void Start() {
        DialogueUI.Instance.OnDialogueEnd += HandleDialogueEnd;
        _npcStoppingDistanceSqr = npcAgent ? npcAgent.stoppingDistance * npcAgent.stoppingDistance : 0f;
        teacherNpc.TryGetComponent(out _teacherAnimator);
        // Start with fade-in effect
        fadeImage.color = new Color(0, 0, 0, 1);
        fadeImage.DOFade(0, missionData.initialFadeDuration).SetEase(Ease.InOutQuad).OnComplete(() => {
            fadeImage.gameObject.SetActive(false);
            AudioManager.Instance.PlayMusicBasedOnScene(SceneManager.GetActiveScene());
        });
    }
    
    public void ObservedUpdate() { // Called every frame by UpdateManager
        if (CurrentCheckpoint >= missionData.checkpoints.Length) return;
        // Move NPC to current checkpoint
        if (_isMovingToCheckpoint && npcAgent.isOnNavMesh) {
            _targetPosition = missionData.checkpoints[CurrentCheckpoint].position;
            npcAgent.SetDestination(_targetPosition);
            float distanceSqr = (npcAgent.transform.position - _targetPosition).sqrMagnitude;

            if (distanceSqr > _npcStoppingDistanceSqr) {
                npcAnimator.SetFloat(SpeedFloat, 1f);// Play walk animation
            } else {
                npcAnimator.SetFloat(SpeedFloat, 0f);// Stop animation
                _isMovingToCheckpoint = false;
                _isWaitingForInteraction = true;
            }
        }

        if (!_isMovingToCheckpoint)// Smoothly rotate NPC to face the target
            SmoothLookAt(
                CurrentCheckpoint >= 0 && missionData.checkpoints[CurrentCheckpoint].action == DialogueMissionData.CheckpointAction.CameraFocus &&
                missionData.checkpoints[CurrentCheckpoint].isTeacherTarget
                    ? teacherNpc.transform
                    : Player.Instance.transform);
    }

    public void StartMission() { // Begins the mission
        MissionStarted = true;
        npcAgent.isStopped = true;
        missionText.text = string.Empty;
        if (missionData.initialDialogue.Length > 0) {// Start with initial dialogue if available
            DialogueUI.Instance.ShowDialogue(missionData.initialDialogue);
        } else {
            CurrentCheckpoint = 0;
            _isMovingToCheckpoint = true;
        }
    }

    private void HandleDialogueEnd(object sender, EventArgs e) {// Called when a dialogue finishes
        npcAgent.isStopped = false;
        // End any active camera focus
        if (_isActionActive) {
            DialogueMissionData.Checkpoint checkpoint = missionData.checkpoints[CurrentCheckpoint];
            if (checkpoint.action == DialogueMissionData.CheckpointAction.CameraFocus) {
                ResetCamera();
                if (!string.IsNullOrEmpty(checkpoint.animatorBool) && checkpoint.isTeacherTarget)
                    _teacherAnimator?.SetBool(checkpoint.animatorBool, false);
                _isActionActive = false;
            }
        }
        // Move to the next checkpoint or activate the guide if it's the last one
        if (CurrentCheckpoint < missionData.checkpoints.Length - 1) {
            CurrentCheckpoint++;
            _isMovingToCheckpoint = true;
        } else {
            DialogueMissionData.Checkpoint checkpoint = missionData.checkpoints[CurrentCheckpoint];
            if (checkpoint.action == DialogueMissionData.CheckpointAction.ActivateGuide) visualGuide.ActivateGuide();
        }
    }
    
    public void OnNpcInteracted() {// Called when player interacts with the NPC
        if (!_isWaitingForInteraction) return;
        _isWaitingForInteraction = false;
        ExecuteCheckpointAction();
    }
    
    private void ExecuteCheckpointAction() {  // Executes the logic defined in the current checkpoint
        npcAgent.isStopped = true;
        DialogueMissionData.Checkpoint checkpoint = missionData.checkpoints[CurrentCheckpoint];

        switch (checkpoint.action) {
            case DialogueMissionData.CheckpointAction.Dialogue:
                if (checkpoint.dialogueLines.Length > 0) {
                    DialogueUI.Instance.ShowDialogue(checkpoint.dialogueLines);
                } else {
                    HandleDialogueEnd(this, EventArgs.Empty);
                }

                break;
            case DialogueMissionData.CheckpointAction.CameraFocus:
                _isActionActive = true;
                if (!string.IsNullOrEmpty(checkpoint.animatorBool) && checkpoint.isTeacherTarget)
                    _teacherAnimator?.SetBool(checkpoint.animatorBool, true);
                SmoothLookAt(teacherNpc ? teacherNpc.transform : Player.Instance.transform);
                FocusCameraOnTarget(teacherNpc.transform);
                
                if (checkpoint.dialogueLines.Length > 0) 
                    DialogueUI.Instance.ShowDialogue(checkpoint.dialogueLines);
                else 
                    HandleDialogueEnd(this, EventArgs.Empty);
                
                break;
            case DialogueMissionData.CheckpointAction.ActivateGuide:
                if (checkpoint.dialogueLines.Length > 0) {
                    DialogueUI.Instance.ShowDialogue(checkpoint.dialogueLines);
                } else {
                    HandleDialogueEnd(this, EventArgs.Empty);
                }
                break;
        }
    }

    private void SmoothLookAt(Transform target) { // Smoothly rotates the NPC to face a target
        if (!target) return;
        
        Vector3 directionToTarget = (target.position - npcAgent.transform.position).normalized;
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero) {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            npcAgent.transform.rotation =
                Quaternion.Slerp(npcAgent.transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    private void FocusCameraOnTarget(Transform target) {// Smoothly rotates the camera to face a target
        if (!target) return;
        
        _originalCameraRotation = playerCamera.transform.rotation;
        Vector3 directionToTarget = (target.transform.position - playerCamera.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
        playerCamera.transform.rotation =
            Quaternion.Slerp(playerCamera.transform.rotation, lookRotation, 5f * Time.deltaTime);
    }

    private void ResetCamera() {// Resets the camera to its original rotation
        playerCamera.transform.rotation = _originalCameraRotation;
    }

    private void OnEnable() => UpdateManager.RegisterObserver(this); // Registers this object to receive update events
    private void OnDisable() => UpdateManager.UnregisterObserver(this); // Unregisters this object from receiving update events

    private void OnDestroy() {  // Cleans up event subscriptions on destruction
        DialogueUI.Instance.OnDialogueEnd -= HandleDialogueEnd;
    }
}

