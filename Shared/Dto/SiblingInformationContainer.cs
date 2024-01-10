using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncToAsync.Shared.Dto
{
    public class SiblingInformationContainer
    {
        
        public SiblingInformationContainer(
            )
        {
        }

        public string Title
        {
            get;
            set;
        } = "";

        public bool Disabled
        {
            get;
            set;
        }
        public CodeLensSibling? Sibling
        {
            get;
            set;
        } = null;

        public static SiblingInformationContainer GetDisabled(CodeLensTarget target)
        {
            return new SiblingInformationContainer
            {
                Disabled = true,
            };
        }

        public static SiblingInformationContainer GetNoSibling(CodeLensTarget target)
        {
            return new SiblingInformationContainer
            {
                Disabled = false,
                Sibling = null
            };
        }

        public static SiblingInformationContainer GetWithSibling(
            CodeLensTarget target,
            CodeLensSibling sibling
            )
        {
            return new SiblingInformationContainer
            {
                Sibling = sibling,
                Title = sibling.MethodName,
                Disabled = false,
            };
        }
    }
}
