using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terminus
{
    public interface IOnBeforeAttachment
    {
        void OnBeforeAttachment(AttachmentInfo attachmentInfo);       
    }

    public interface IOnAfterAttachment
    {
        void OnAfterAttachment(AttachmentInfo attachmentInfo);
    }

    public interface IOnBeforeDetachment
    {
        void OnBeforeDetachment(AttachmentInfo attachmentInfo);
    }

    public interface IOnAfterDetachment
    {
        void OnAfterDetachment(AttachmentInfo attachmentInfo);
    }
}