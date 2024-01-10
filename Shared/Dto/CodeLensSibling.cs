using System;

namespace SyncToAsync.Shared.Dto
{
    public class CodeLensSibling
    {
        public Guid ProjectGuid
        {
            get;
            set;
        }
        public Guid DocumentGuid
        {
            get;
            set;
        }
        public string FilePath
        {
            get;
            set;
        }
        public bool IsSourceGeneratedDocument
        {
            get;
            set;
        }
        public string MethodName
        {
            get;
            set;
        }
        public string MethodBody
        {
            get;
            set;
        }
        public int? MethodSpanStart
        {
            get;
            set;
        }
        public int? MethodSpanLength
        {
            get;
            set;
        }

        public CodeLensSibling(
            Guid projectGuid,
            Guid documentGuid,
            string filePath,
            bool isSourceGeneratedDocument,
            string methodName,
            string methodBody,
            int? methodSpanStart,
            int? methodSpanLength
            )
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or empty.", nameof(filePath));
            }

            if (methodName is null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            ProjectGuid = projectGuid;
            DocumentGuid = documentGuid;
            FilePath = filePath;
            IsSourceGeneratedDocument = isSourceGeneratedDocument;
            MethodName = methodName;
            MethodBody = methodBody;
            MethodSpanStart = methodSpanStart;
            MethodSpanLength = methodSpanLength;
        }
    }
}
