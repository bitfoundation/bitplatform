namespace Bit.Client.Web.BlazorUI
{
    public enum BitFileUploadStatus
    {
        /// <summary>
        /// File uploading progress is pended because the server cannot be contacted.
        /// </summary>
        Pending,

        /// <summary>
        /// File uploading is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// File uploading progress is paused by the user.
        /// </summary>
        Paused,

        /// <summary>
        /// File uploading progress is canceled by the user.
        /// </summary>
        Canceled,

        /// <summary>
        /// The file is successfully uploaded.
        /// </summary>
        Completed,

        /// <summary>
        /// The file has a problem and progress is failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The uploaded file removed by the user.
        /// </summary>
        Removed,

        /// <summary>
        /// The file removal failed.
        /// </summary>
        RemoveFailed,

        /// <summary>
        /// The type of uploaded file is not allowed.
        /// </summary>
        NotAllowed
    }
}
