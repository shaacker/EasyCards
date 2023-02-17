using System.Collections.Generic;
using BepInEx.Logging;
using EasyCards.Models.Templates;
using RogueGenesia.Data;

namespace EasyCards.Helpers;

public static class Localization
{
    private static ManualLogSource Logger => EasyCards.Instance.Log;
    public static List<LocalizationData> GetTranslations(Dictionary<string, string> translations)
    {
        var result = new List<LocalizationData>();

        foreach (var (localizationKey, translation) in translations)
        {
            var ld = new LocalizationData
            {
                Key = localizationKey,
                Value = translation
            };

            result.Add(ld);
        }

        return result;
    }

    public static List<LocalizationData> GetNameTranslations(CardTemplate cardTemplate)
    {
        return GetTranslations(cardTemplate.NameLocalization);
    }

    public static List<LocalizationData> GetDescriptionTranslations(CardTemplate cardTemplate)
    {
        return GetTranslations(cardTemplate.DescriptionLocalization);
    }

    public static void PostProcessDescriptions(Dictionary<string, SoulCardScriptableObject> allCards, Dictionary<string, CardTemplate> addedCards)
    {
        Logger.LogInfo($"=== Post processing descriptions for {addedCards.Count} cards ===");

        foreach (var cardName in addedCards.Keys)
        {
            var cardTemplate = addedCards[cardName];
            if (cardTemplate.DescriptionLocalization.Count == 0)
                continue;

            var cardScso = allCards[cardName];
            var translations = GetDescriptionTranslations(cardTemplate);
            Logger.LogInfo($"\tGot {translations.Count} description translations for {cardName}");

            foreach (var translation in translations)
            {
                cardScso.DescriptionOverride.Add(translation);
            }
        }
    }
}
