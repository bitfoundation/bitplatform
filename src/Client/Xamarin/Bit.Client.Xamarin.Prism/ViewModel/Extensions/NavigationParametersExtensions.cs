﻿using System;

namespace Prism.Navigation
{
    public static class NavigationParametersExtensions
    {
        public static bool TryGetNavigationMode(this INavigationParameters parameters, out NavigationMode navigationMode)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            INavigationParametersInternal internalParameters = (INavigationParametersInternal)parameters;

            if (internalParameters.ContainsKey("__NavigationMode"))
            {
                navigationMode = internalParameters.GetValue<NavigationMode>("__NavigationMode");
                return true;
            }

            navigationMode = NavigationMode.New;
            return false;
        }
    }
}
