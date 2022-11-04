using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyCards.Helpers;
using EasyCards.Models.Templates;
using EasyCards.Services;
using Microsoft.Extensions.Logging;
using ModGenesia;
using RogueGenesia.Data;

namespace EasyCards.Bootstrap;

public sealed class CardLoader : ICardLoader
{
    public CardLoader(ILogger<CardLoader> logger, IJsonDeserializer jsonDeserializer, IDebugHelper debugHelper,
        ISpriteLoader spriteLoader, ICardRepository cardRepository)
    {
        Logger = logger;
        _jsonDeserializer = jsonDeserializer;
        _debugHelper = debugHelper;
        _spriteLoader = spriteLoader;
        _cardRepository = cardRepository;
    }

    public int LoadOrder => 75;

    private ILogger Logger { get; }
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IDebugHelper _debugHelper;
    private readonly ISpriteLoader _spriteLoader;
    private readonly ICardRepository _cardRepository;

    private readonly Dictionary<string, CardTemplate> _successFullyLoadedCards = new();

    public void Initialize()
    {
        var jsonFiles = Directory.GetFiles(Paths.Data, "*.json");
        foreach (var jsonFile in jsonFiles)
        {
            try
            {
                AddCardsFromFile(jsonFile);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unable to load cards from file {jsonFile}: {ex}", jsonFile, ex);
            }
        }
    }

    public Dictionary<string, CardTemplate> GetLoadedCards() => _successFullyLoadedCards;

    public void AddCardsFromFile(string fileName)
    {
        if (!File.Exists(fileName))
        {
            Logger.LogError("File does not exist: {fileName}", fileName);
        }

        Logger.LogInformation("Loading cards from file {fileName}", fileName);

        var json = File.ReadAllText(fileName);
        var templateFile = _jsonDeserializer.Deserialize<TemplateFile>(json);

        Logger.LogInformation($"Loaded {templateFile.Stats.Count} cards");

        var modSource = templateFile.ModSource ?? MyPluginInfo.PLUGIN_NAME;

        foreach (var cardTemplate in templateFile.Stats)
        {
            try
            {
                var soulCardData = ConvertCardTemplate(modSource, cardTemplate);
                Logger.LogInformation($"Adding card {cardTemplate.Name}");
                ModGenesia.ModGenesia.AddCustomStatCard(cardTemplate.Name, soulCardData);
                _successFullyLoadedCards.Add(cardTemplate.Name, cardTemplate);
            }
            catch (Exception ex)
            {
                Logger.LogInformation(ex, $"Error adding {cardTemplate.Name}: {ex}");
            }
        }
    }

    private SoulCardCreationData ConvertCardTemplate(string modSource, CardTemplate cardTemplate)
    {
        Logger.LogDebug($"Converting {cardTemplate.Name}");
        var soulCardData = new SoulCardCreationData();

        soulCardData.ModSource = modSource;

        var texturePath = Path.Combine(Paths.Assets, cardTemplate.TexturePath);

        var sprite = _spriteLoader.LoadSprite(texturePath);

        if (sprite)
        {
            soulCardData.Texture = sprite;
        }
        else
        {
            Logger.LogError($"Unable to load sprite from {texturePath}");
        }

        soulCardData.Rarity = (CardRarity)(int)cardTemplate.Rarity;

        var tags = (CardTag)cardTemplate.Tags.Aggregate(0, (current, tag) => current | (int)tag);

        soulCardData.Tags = tags;
        soulCardData.DropWeight = cardTemplate.DropWeight;
        soulCardData.LevelUpWeight = cardTemplate.LevelUpWeight;
        soulCardData.MaxLevel = cardTemplate.MaxLevel;

        soulCardData.StatsModifier = cardTemplate.CreateStatsModifier();

        if (cardTemplate.NameLocalization.Count > 0)
        {
            var nameLocalizations = Localization.GetNameTranslations(cardTemplate);
            foreach (var localization in nameLocalizations)
            {
                soulCardData.NameOverride.Add(localization);
            }
        }
        else
        {
            Logger.LogWarning($"No Name localizations provided for {cardTemplate.Name}!");
        }

        if (cardTemplate.DescriptionLocalization.Count > 0)
        {
            var nameLocalizations = Localization.GetDescriptionTranslations(cardTemplate);
            foreach (var localization in nameLocalizations)
            {
                soulCardData.DescriptionOverride.Add(localization);
            }
        }

        soulCardData.CardExclusion = cardTemplate.BanishesCardsByName.ToArray();
        soulCardData.CardToRemove = cardTemplate.RemovesCards.ToArray();
        if (cardTemplate.BanishesCardsWithStatsOfType.Count > 0)
        {
            soulCardData.CardWithStatsToBan = this.ConvertStringsToStatsTypes(cardTemplate.BanishesCardsWithStatsOfType);
        }

        soulCardData.CardRequirement = cardTemplate.RequiresAny?.ToRequirementList();
        soulCardData.CardHardRequirement = cardTemplate.RequiresAll?.ToRequirementList();

        return soulCardData;
    }

    private StatsType[] ConvertStringsToStatsTypes(List<string> statNames)
    {
        var result = new HashSet<StatsType>();
        foreach (var statName in statNames)
        {
            if (Enum.TryParse<StatsType>(statName, true, out var stat))
            {
                result.Add(stat);
            }
            else
            {
                Logger.LogWarning($"Could not convert stat: {statName} ");
            }
        }

        return result.ToArray();
    }
}
