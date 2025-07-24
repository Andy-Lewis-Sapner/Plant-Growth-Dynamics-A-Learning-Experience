using System;
using UnityEngine;

/// <summary>
/// Represents the data structure for a dialogue mission within the game.
/// This ScriptableObject stores information about mission checkpoints,
/// associated dialogues, actions, and initial mission setup data.
/// </summary>
[CreateAssetMenu(fileName = "DialogueMissionData", menuName = "Scriptable Objects/DialogueMissionData", order = 0)]
public class DialogueMissionData : ScriptableObject {
    /// Represents an action to focus the camera on a specific target during a mission checkpoint.
    /// When this action is triggered, it adjusts the camera view to emphasize a designated target,
    /// such as the teacher NPC or the player. This can be used to draw the player's attention to
    /// specific characters or events.
    [Serializable]
    public enum CheckpointAction {
        Dialogue,
        CameraFocus,
        ActivateGuide,
        EndMission
    }

    /// <summary>
    /// Represents a checkpoint within a dialogue mission. Contains the position, associated dialogue,
    /// specified action to execute, and other contextual data related to the checkpoint.
    /// </summary>
    [Serializable]
    public struct Checkpoint {
        /// <summary>
        /// Represents the position of a checkpoint in the mission, defined as a <see cref="Vector3"/>.
        /// This variable specifies the world-space coordinates for the checkpoint.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Represents an array of dialogue lines relevant to a specific checkpoint in a mission.
        /// </summary>
        public string[] dialogueLines;

        /// <summary>
        /// Represents the type of action to be executed for a mission checkpoint.
        /// </summary>
        public CheckpointAction action;

        /// <summary>
        /// Indicates whether the current checkpoint's action or focus is specifically targeting the teacher NPC.
        /// </summary>
        public bool isTeacherTarget;

        /// <summary>
        /// The name of a boolean parameter in an Animator that is used during a specific checkpoint action,
        /// such as enabling or disabling an animation (e.g., "Talk") related to a mission task.
        /// </summary>
        [Tooltip("Animator bool to set during action (such as Talk)")]
        public string animatorBool;
    }

    /// <summary>
    /// Represents the name of the mission within the DialogueMissionData.
    /// Used to identify and reference the mission in the game or related systems.
    /// </summary>
    public string missionName;

    /// <summary>
    /// Represents an array of checkpoints for a mission.
    /// Each checkpoint contains data such as position, dialogue lines, actions to perform, and additional properties
    /// that define its behavior or interaction with game logic.
    /// </summary>
    public Checkpoint[] checkpoints;

    /// <summary>
    /// Represents the introductory lines of dialogue displayed at the start of a mission.
    /// </summary>
    public string[] initialDialogue;

    /// Represents the duration, in seconds, for the fade-in effect at the start of a mission.
    /// Used to control the initial visual transition by fading in associated UI or scene elements smoothly.
    [Tooltip("Seconds to fade in at mission start")]
    public float initialFadeDuration = 2f;
}