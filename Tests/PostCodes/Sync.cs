﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Community;
using Newtonsoft.Json;
using Place;
using Province;
using ProvinceCommunity;
using State;
using StateCommunity;
using StateProvince;
using StateProvinceCommunity;
using Xunit;

public class Sync
{
    [Fact]
    public async Task SyncCountryData()
    {
        var currentDirectory = Environment.CurrentDirectory;
        var slnPath = Path.GetFullPath(Path.Combine(currentDirectory, "../../../../"));
        var tempPath = Path.GetFullPath(Path.Combine(slnPath, "temp"));
        var jsonIndentedPath = Path.GetFullPath(Path.Combine(slnPath, "json_indented"));
        var jsonPath = Path.GetFullPath(Path.Combine(slnPath, "json"));
        var countriesPath = Path.GetFullPath(Path.Combine(slnPath, "countries.txt"));
        var allCountriesZipPath = Path.Combine(tempPath, "allCountries.zip");
        await Downloader.DownloadFile(allCountriesZipPath);
        var allCountriesTxtPath = Path.Combine(tempPath, "allCountries.txt");
        File.Delete(allCountriesTxtPath);
        ZipFile.ExtractToDirectory(allCountriesZipPath, tempPath);

        var list = RowReader.ReadRows(allCountriesTxtPath).ToList();
        var jsonSerializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
        };
        var groupByCountry = list.GroupBy(x => x.CountryCode).ToList();
        File.Delete(countriesPath);
        File.WriteAllLines(countriesPath, groupByCountry.Select(x => x.Key.ToLower()));
        WriteRows(jsonPath, jsonSerializer, groupByCountry);

        jsonSerializer.Formatting = Formatting.Indented;
        WriteRows(jsonIndentedPath, jsonSerializer, groupByCountry);
    }

    void WriteRows(string jsonPath, JsonSerializer jsonSerializer, List<IGrouping<string, Row>> groupByCountry)
    {
        IoHelpers.PurgeDirectory(jsonPath);
        foreach (var group in groupByCountry)
        {
            var countryJsonFilePath = Path.Combine(jsonPath, @group.Key.ToLower() + ".json.txt");
            using (var fileStream = File.OpenWrite(countryJsonFilePath))
            using (var textWriter = new StreamWriter(fileStream))
            using (var jsonTextWriter = new JsonTextWriter(textWriter))
            {
                jsonTextWriter.IndentChar = ' ';
                jsonTextWriter.Indentation = 1;
                ProcessCountry(@group.ToList(), o => { jsonSerializer.Serialize(jsonTextWriter, o); });
            }
        }
    }

    void ProcessCountry(List<Row> rows, Action<object> action)
    {
        var hasState= rows.Any(_=>_.State != null);
        var hasProvince = rows.Any(_=>_.Province != null);
        var hasCommunity = rows.Any(_=>_.Community != null);

        if (hasState && hasProvince && hasCommunity)
        {
            StateProvinceCommunitySerializer.Serialize(rows, action);
            return;
        }
        if (hasState && hasProvince)
        {
            StateProvinceSerializer.Serialize(rows, action);
            return;
        }
        if (hasState && hasCommunity)
        {
            StateCommunitySerializer.Serialize(rows, action);
            return;
        }
        if (hasProvince && hasCommunity)
        {
            ProvinceCommunitySerializer.Serialize(rows, action);
            return;
        }
        if (hasState)
        {
            StateSerializer.Serialize(rows, action);
            return;
        }
        if (hasProvince)
        {
            ProvinceSerializer.Serialize(rows, action);
            return;
        }
        if (hasCommunity)
        {
            CommunitySerializer.Serialize(rows, action);
            return;
        }
        PlaceSerializer.Serialize(rows, action);
    }
}