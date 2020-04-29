﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Responses
{
    /// <summary>
    /// Multi locale Template Manager for language generation. This template manager will enumerate multi-locale template files and will select
    /// the appropriate template using the current culture to perform template evaluation.
    /// </summary>
    public class LocaleTemplateManager : MultiLanguageLG
    {
        private string _fallbackLocale;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocaleTemplateManager"/> class.
        /// </summary>
        /// <param name="localeTemplateFiles">A dictionary of locale and template file.</param>
        /// <param name="fallbackLocale">The default fallback locale to use.</param>
        public LocaleTemplateManager(Dictionary<string, string> localeTemplateFiles, string fallbackLocale)
            : base(fallbackLocale == null ? localeTemplateFiles : new Dictionary<string, string>(localeTemplateFiles) { { string.Empty, localeTemplateFiles[fallbackLocale] } })
        {
            foreach (KeyValuePair<string, string> filePerLocale in localeTemplateFiles)
            {
                TemplateEnginesPerLocale[filePerLocale.Key] = Templates.ParseFile(filePerLocale.Value);
            }

            // only throw when fallbackLocale is empty string
            if (fallbackLocale != null && (fallbackLocale == string.Empty || fallbackLocale.Trim() == string.Empty))
            {
                throw new ArgumentException($"{nameof(fallbackLocale)} shouldn't be empty string. If you don't want to set it, please set it to null.");
            }

            _fallbackLocale = fallbackLocale;
        }

        public Dictionary<string, Templates> TemplateEnginesPerLocale { get; set; } = new Dictionary<string, Templates>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Create an activity through Language Generation using the thread culture or provided override.
        /// </summary>
        /// <param name="templateName">Language Generation template.</param>
        /// <param name="data">Data for Language Generation to use during response generation.</param>
        /// <param name="localeOverride">Optional override for locale.</param>
        /// <returns>Activity.</returns>
        /// <remarks>
        /// The InputHint property of the returning activity is set to be null if it's acceptingInput so
        /// when the activity is being used in a prompt it'll be set to expectingInput.
        /// </remarks>
        public Activity GenerateActivityForLocale(string templateName, object data = null, string localeOverride = null)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            // only throw when localeOverride is empty string
            if (localeOverride != null && (localeOverride == string.Empty || localeOverride.Trim() == string.Empty))
            {
                throw new ArgumentException($"{nameof(localeOverride)} shouldn't be empty string. If you don't want to set it, please set it to null.");
            }

            var locale = localeOverride ?? CultureInfo.CurrentUICulture.Name ?? _fallbackLocale;

            return ActivityFactory.FromObject(Generate($"${{{templateName}()}}", data, locale));
        }

        /// <summary>
        /// Get localized templates based on CultureInfo.
        /// </summary>
        /// <returns>Templates.</returns>
        public Templates GetTemplates()
        {
            // Get cognitive models for locale
            var locale = CultureInfo.CurrentUICulture.Name.ToLower();

            var templates = TemplateEnginesPerLocale.ContainsKey(locale)
                ? TemplateEnginesPerLocale[locale]
                : TemplateEnginesPerLocale.Where(key => key.Key.StartsWith(locale.Substring(0, 2))).FirstOrDefault().Value
                  ?? throw new Exception($"There's no matching locale for '{locale}' or its root language '{locale.Substring(0, 2)}'. " +
                                         "Please review your available locales in your LG files.");

            return templates;
        }
    }
}