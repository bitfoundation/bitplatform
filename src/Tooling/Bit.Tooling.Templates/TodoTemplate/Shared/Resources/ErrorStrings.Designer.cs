﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TodoTemplate.Shared.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class ErrorStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ErrorStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TodoTemplate.Shared.Resources.ErrorStrings", typeof(ErrorStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your account has been locked out. Try again in 5 minutes..
        /// </summary>
        public static string AccountIsLockedOut {
            get {
                return ResourceManager.GetString("AccountIsLockedOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid request.
        /// </summary>
        public static string BadRequestException {
            get {
                return ResourceManager.GetString("BadRequestException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request could not be processed because of conflict in the request.
        /// </summary>
        public static string ConflicException {
            get {
                return ResourceManager.GetString("ConflicException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while removing file.
        /// </summary>
        public static string FileRemoveFailed {
            get {
                return ResourceManager.GetString("FileRemoveFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while uploading file.
        /// </summary>
        public static string FileUploadFailed {
            get {
                return ResourceManager.GetString("FileUploadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access to the requested resource is forbidden.
        /// </summary>
        public static string ForbiddenException {
            get {
                return ResourceManager.GetString("ForbiddenException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid username and / or password.
        /// </summary>
        public static string InvalidUserNameAndOrPassword {
            get {
                return ResourceManager.GetString("InvalidUserNameAndOrPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Request data is not valid.
        /// </summary>
        public static string ResourceValidationException {
            get {
                return ResourceManager.GetString("ResourceValidationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while communicating with server.
        /// </summary>
        public static string RestException {
            get {
                return ResourceManager.GetString("RestException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Role could not be found.
        /// </summary>
        public static string RoleCouldNotBeFound {
            get {
                return ResourceManager.GetString("RoleCouldNotBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to To Do item could not be found.
        /// </summary>
        public static string ToDoItemCouldNotBeFound {
            get {
                return ResourceManager.GetString("ToDoItemCouldNotBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your request lacks valid authentication credentials.
        /// </summary>
        public static string UnauthorizedException {
            get {
                return ResourceManager.GetString("UnauthorizedException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown error has occurred.
        /// </summary>
        public static string UnknownException {
            get {
                return ResourceManager.GetString("UnknownException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User could not be found.
        /// </summary>
        public static string UserCouldNotBeFound {
            get {
                return ResourceManager.GetString("UserCouldNotBeFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User image could not be found.
        /// </summary>
        public static string UserImageCouldNotBeFound {
            get {
                return ResourceManager.GetString("UserImageCouldNotBeFound", resourceCulture);
            }
        }
    }
}
