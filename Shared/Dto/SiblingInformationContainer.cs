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
        public bool Idle
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
                Idle = false
            };
        }

        public static SiblingInformationContainer GetIdle(CodeLensTarget target)
        {
            return new SiblingInformationContainer
            {
                Disabled = false,
                Idle = true
            };
        }

        public static SiblingInformationContainer GetNoSibling(CodeLensTarget target)
        {
            return new SiblingInformationContainer
            {
                Disabled = false,
                Idle = false,
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
                Idle = false,
                Title = sibling.MethodName,
                Disabled = false,
            };
        }
    }
}
