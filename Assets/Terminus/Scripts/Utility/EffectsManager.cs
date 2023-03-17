using UnityEngine;
using System.Collections;

namespace Terminus 
{
	/// <summary>
	/// Manages creating effects on attachment and detachment of <see cref="Connector"/>s.
	/// </summary>
	/// <remarks>
	/// Can be placed on GameObject containing <see cref="TerminusObject"/>, <see cref="Connector"/> or <see cref="Manager.globalMessagesReciever"/>.
	/// This component is an example of using Terminus messaging.
	/// </remarks>
	/// <seealso cref="Settings.LayerPortOptions.sendMessagesToPorts"/> 
	/// <seealso cref="Settings.LayerPortOptions.sendMessagesToOwnerObjects"/> 
	/// <seealso cref="Settings.LayerPortOptions.sendMessagesToGameObject"/> 
	[AddComponentMenu("Terminus modular system/Utility/Effects and sounds manager")]
	public class EffectsManager : MonoBehaviour, IOnBeforeAttachment, IOnBeforeDetachment {

		/// <summary>
		/// GameObject to instantiate on attachment.
		/// </summary>
		public GameObject attachmentEffect;
		/// <summary>
		/// Destroy instantiated <see cref="EffectsManager.attachmentEffect"/> after this amount of time (in seconds).
		/// </summary>
		public float attachmentEffectTimer;
		/// <summary>
		/// AudioClip to play on attachment;
		/// </summary>
		public AudioClip attachmentAudioClip;
		/// <summary>
		/// GameObject to instantiate on sideway attachment.
		/// </summary>
		public GameObject sidewayAttachmentEffect;
		/// <summary>
		/// Destroy instantiated <see cref="EffectsManager.sidewayAttachmentEffect"/> after this amount of time (in seconds).
		/// </summary>
		public float sidewayAttachmentEffectTimer;
		/// <summary>
		/// AudioClip to play on sideway attachment;
		/// </summary>
		public AudioClip sidewayAttachmentAudioClip;
		/// <summary>
		/// GameObject to instantiate on detachment.
		/// </summary>
		public GameObject detachmentEffect;
		/// <summary>
		/// Destroy instantiated <see cref="EffectsManager.detachmentEffect"/> after this amount of time (in seconds).
		/// </summary>
		public float detachmentEffectTimer;
		/// <summary>
		/// AudioClip to play on detachment;
		/// </summary>
		public AudioClip detachmentAudioClip;
		

		public void OnBeforeAttachment(AttachmentInfo info)
		{            
			if (Application.isPlaying)
			{                
                if (info.attachmentType == AttachmentInfo.Types.child || info.attachmentType == AttachmentInfo.Types.parent)
				{         
                    if (attachmentEffect != null)
					{
						GameObject obj = (GameObject)Instantiate(attachmentEffect,info.selfConnector.globalPosition,info.selfConnector.globalRotation);
						Destroy(obj,attachmentEffectTimer);
					}
					if (attachmentAudioClip != null)
						AudioSource.PlayClipAtPoint(attachmentAudioClip,info.selfConnector.globalPosition);
				}
				else if (info.attachmentType == AttachmentInfo.Types.sideway)
				{
					if (sidewayAttachmentEffect != null)
					{
						GameObject obj = (GameObject)Instantiate(sidewayAttachmentEffect,info.selfConnector.globalPosition,info.selfConnector.globalRotation);
						Destroy(obj,sidewayAttachmentEffectTimer);
					}
					if (sidewayAttachmentAudioClip != null)
						AudioSource.PlayClipAtPoint(sidewayAttachmentAudioClip,info.selfConnector.globalPosition);
				}
			}
		}

        public void OnBeforeDetachment(AttachmentInfo info)
		{
			if (Application.isPlaying && !info.otherConnector.owner.destroyFlag && !info.selfConnector.owner.destroyFlag)
			{
				if (detachmentEffect != null)
				{
					GameObject obj = (GameObject)Instantiate(detachmentEffect,info.selfConnector.globalPosition,info.selfConnector.globalRotation);
					Destroy(obj,detachmentEffectTimer);
				}
				if (detachmentAudioClip != null)
					AudioSource.PlayClipAtPoint(detachmentAudioClip,info.selfConnector.globalPosition);
			}
		}

	}
}