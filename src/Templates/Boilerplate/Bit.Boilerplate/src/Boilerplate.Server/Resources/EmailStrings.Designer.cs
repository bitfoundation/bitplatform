﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Boilerplate.Server.Resources {
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
    public class EmailStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal EmailStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Boilerplate.Server.Resources.EmailStrings", typeof(EmailStrings).Assembly);
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
        ///   Looks up a localized string similar to Boilerplate.
        /// </summary>
        public static string AppName {
            get {
                return ResourceManager.GetString("AppName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Boilerplate - Confirm your email address.
        /// </summary>
        public static string ConfirmationEmailSubject {
            get {
                return ResourceManager.GetString("ConfirmationEmailSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copy this code and use it in the login page..
        /// </summary>
        public static string CopyTokenNote {
            get {
                return ResourceManager.GetString("CopyTokenNote", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Or click on the following link to confirm this email address..
        /// </summary>
        public static string EmailConfirmationMessageBodyLink {
            get {
                return ResourceManager.GetString("EmailConfirmationMessageBodyLink", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Confirm your email address by entering the number below in the app..
        /// </summary>
        public static string EmailConfirmationMessageBodyToken {
            get {
                return ResourceManager.GetString("EmailConfirmationMessageBodyToken", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You&apos;re receiving this email because recently you have registered {0} to your Boilerplate account..
        /// </summary>
        public static string EmailConfirmationMessageSubtitle {
            get {
                return ResourceManager.GetString("EmailConfirmationMessageSubtitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to If you&apos;re not the one requested the reset password, you can ignore this email..
        /// </summary>
        public static string ResetPasswordBody {
            get {
                return ResourceManager.GetString("ResetPasswordBody", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Boilerplate - Reset your password.
        /// </summary>
        public static string ResetPasswordEmailSubject {
            get {
                return ResourceManager.GetString("ResetPasswordEmailSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Or you can click on the following link to try to reset your password..
        /// </summary>
        public static string ResetPasswordLinkMessage {
            get {
                return ResourceManager.GetString("ResetPasswordLinkMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Someone has requested a token to change your password..
        /// </summary>
        public static string ResetPasswordSubtitle {
            get {
                return ResourceManager.GetString("ResetPasswordSubtitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hello {0}.
        /// </summary>
        public static string ResetPasswordTitle {
            get {
                return ResourceManager.GetString("ResetPasswordTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You can copy the below token and enter it in the reset password page..
        /// </summary>
        public static string ResetPasswordTokenMessage {
            get {
                return ResourceManager.GetString("ResetPasswordTokenMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Reset your password.
        /// </summary>
        public static string ResetYourPassword {
            get {
                return ResourceManager.GetString("ResetYourPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Boilerplate - 2FA token.
        /// </summary>
        public static string TfaTokenEmailSubject {
            get {
                return ResourceManager.GetString("TfaTokenEmailSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Hello dear {0}.
        /// </summary>
        public static string TfaTokenHello {
            get {
                return ResourceManager.GetString("TfaTokenHello", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Here is your new 2FA token to login:.
        /// </summary>
        public static string TfaTokenMessage {
            get {
                return ResourceManager.GetString("TfaTokenMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Welcome to Boilerplate!.
        /// </summary>
        public static string WelcomeToApp {
            get {
                return ResourceManager.GetString("WelcomeToApp", resourceCulture);
            }
        }
    }
}
