using System.Collections.Generic;
using XboxDownload.Helpers.Resources;

namespace XboxDownload.Models.SpeedTest;

public static class SpeedTestDataBuilder
{
    public static IEnumerable<LocationFilter> BuildLocationFilters() =>
    [
        new LocationFilter("ChinaTelecom", ["电信", "電信", "Telecom"], ResourceHelper.GetString("SpeedTest.ChinaTelecom")),
        new LocationFilter("ChinaUnicom", ["联通", "聯通", "Unicom"], ResourceHelper.GetString("SpeedTest.ChinaUnicom")),
        new LocationFilter("ChinaMobile", ["移动", "移動", "Mobile"], ResourceHelper.GetString("SpeedTest.ChinaMobile")),
        new LocationFilter("HongKong", ["香港", "HK,", "Hong Kong"], ResourceHelper.GetString("SpeedTest.HongKong")),
        new LocationFilter("Taiwan", ["台湾", "台灣", "TW,", "Taiwan"], ResourceHelper.GetString("SpeedTest.Taiwan")),
        new LocationFilter("Japan", ["日本", "JP,", "Japan"], ResourceHelper.GetString("SpeedTest.Japan")),
        new LocationFilter("SouthKorea", ["韩国", "韓國", "KR,", "South Korea"], ResourceHelper.GetString("SpeedTest.SouthKorea")),
        new LocationFilter("Singapore", ["新加坡", "SG,", "Singapore"], ResourceHelper.GetString("SpeedTest.Singapore")),
        new LocationFilter("Other", [], ResourceHelper.GetString("SpeedTest.Other")),
    ];

    public static IEnumerable<ImportOption> BuildImportOptions() =>
    [
        new ImportOption("Select", "", ResourceHelper.GetString("SpeedTest.ImportIp.Select.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.Select.Hint")),
        new ImportOption("Akamai", "Akamai", ResourceHelper.GetString("SpeedTest.ImportIp.Akamai.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.Akamai.Hint")),
        new ImportOption("AkamaiV2", "Akamai", ResourceHelper.GetString("SpeedTest.ImportIp.AkamaiV2.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.AkamaiV2.Hint")),
        new ImportOption("AkamaiV6", "Akamai", ResourceHelper.GetString("SpeedTest.ImportIp.AkamaiV6.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.AkamaiV6.Hint")),
        new ImportOption("XboxCn1", "XboxCn1", ResourceHelper.GetString("SpeedTest.ImportIp.XboxCn1.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.XboxCn1.Hint")),
        new ImportOption("XboxCn2", "XboxCn2", ResourceHelper.GetString("SpeedTest.ImportIp.XboxCn2.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.XboxCn2.Hint")),
        new ImportOption("XboxApp", "XboxApp", ResourceHelper.GetString("SpeedTest.ImportIp.XboxApp.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.XboxApp.Hint")),
        new ImportOption("Ps", "Ps", ResourceHelper.GetString("SpeedTest.ImportIp.Ps.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.Ps.Hint")),
        new ImportOption("UbisoftCn", "UbisoftCn", ResourceHelper.GetString("SpeedTest.ImportIp.UbisoftCn.Display"), ResourceHelper.GetString("SpeedTest.ImportIp.UbisoftCn.Hint")),
    ];

    public static IEnumerable<SpeedTestFile> BuildSpeedTestFiles() =>
    [
        new SpeedTestFile("XboxCn1-1", "XboxCn1", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn1-1"), "http://assets1.xboxlive.cn/Z/routing/extraextralarge.txt"),
        new SpeedTestFile("XboxCn1-2", "XboxCn1", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn1-2"), "http://assets1.xboxlive.cn/8/5a77c76d-24ec-48f0-95a6-1c5887a2ecab/0698b936-d300-4451-b9a0-0be0514bbbe5/1.4193.64609.0.9056b6b3-cbce-48d6-bd0e-ed8e27ca3e9d/Microsoft.254428597CFE2_1.4193.64609.0_neutral__8wekyb3d8bbwe_xs.xvc"),
        new SpeedTestFile("XboxCn1-3", "XboxCn1", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn1-3"), "http://assets1.xboxlive.cn/Z/976d47bb-2f32-44c2-a875-b5b0967c7ac7/3d263e92-93cd-4f9b-90c7-5438150cecbf/3.688.44.0.ad877704-6fcb-44eb-8c11-e195610ec1de/Microsoft.624F8B84B80_3.688.44.0_x64__8wekyb3d8bbwe.msixvc"),
        new SpeedTestFile("XboxCn1-4", "XboxCn1", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn1-4"), "http://assets1.xboxlive.cn/8/82e2c767-56a2-4cff-9adf-bc901fd81e1a/1e66a3e7-2f7b-461c-9f46-3ee0aec64b8c/1.1.967.0.4e71a28b-d845-42e5-86bf-36afdd5eb82f/Microsoft.HalifaxBaseGame_1.1.967.0_x64__8wekyb3d8bbwe.msixvc"),

        new SpeedTestFile("XboxCn2-1", "XboxCn2", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn2-1"), "http://dlassets.xboxlive.cn/public/content/1b5a4a08-06f0-49d6-b25f-d7322c11f3c8/372e2966-b158-4488-8bc8-15ef23db1379/1.5.0.1018.88cd7a5d-f56a-40c7-afd8-85cd4940b891/ACUEU771E1BF7_1.5.0.1018_x64__b6krnev7r9sf8"),
        new SpeedTestFile("XboxCn2-2", "XboxCn2", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn2-2"), "http://dlassets.xboxlive.cn/public/content/1d6640d3-3441-42bd-bffd-953d7d09ff5c/26213de4-885d-4eaa-a433-ed5157116507/1.2.1.0.89417ea8-51b5-408c-9283-60c181763a39/Microsoft.Max_1.2.1.0_neutral__ph1m9x8skttmg"),
        new SpeedTestFile("XboxCn2-3", "XboxCn2", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn2-3"), "http://dlassets.xboxlive.cn/public/content/77d0d59a-34b7-4482-a1c7-c0abbed17de2/db7a9163-9c5e-43a8-b8bf-fe0208149792/1.0.0.3.65565c9c-8a1e-438a-b714-2d9965f0485b/ChildOfLight_1.0.0.3_x64__b6krnev7r9sf8"),
        new SpeedTestFile("XboxCn2-4", "XboxCn2", ResourceHelper.GetString("SpeedTest.TestFile.XboxCn2-4"), "http://dlassets.xboxlive.cn/public/content/1c4b6e60-b2e3-420c-a8a8-540fb14c9286/57f7a51d-e6c2-42b2-967b-6f075e1923a7/1.0.0.5.acd29c4f-6d78-41c8-a705-90de47b8273b/SHPUPWW446612E0_1.0.0.5_x64__zjr0dfhgjwvde"),

        new SpeedTestFile("XboxApp-1", "XboxApp", ResourceHelper.GetString("SpeedTest.TestFile.XboxApp-1"), "986a47b3-0085-4c0c-b3b3-3b806f969b00|MsixBundle|9MV0B5HZVK9Z"),
        new SpeedTestFile("XboxApp-2", "XboxApp", ResourceHelper.GetString("SpeedTest.TestFile.XboxApp-2"), "64293252-5926-453c-9494-2d4021f1c78d|MsixBundle|9WZDNCRFJBMP"),
        new SpeedTestFile("XboxApp-3", "XboxApp", ResourceHelper.GetString("SpeedTest.TestFile.XboxApp-3"), "e0229546-200d-4c66-a693-df9bf799635f|EAppxBundle|9PNQKHFLD2WQ"),
        new SpeedTestFile("XboxApp-4", "XboxApp", ResourceHelper.GetString("SpeedTest.TestFile.XboxApp-4"), "4828c82e-7fe6-4d95-9572-20bbe9721c86|EAppx|9NBLGGH4PBBM"),

        new SpeedTestFile("Ps-1", "Ps", ResourceHelper.GetString("SpeedTest.TestFile.Ps-1"), "http://gst.prod.dl.playstation.net/networktest/get_192m"),
        new SpeedTestFile("Ps-2", "Ps", ResourceHelper.GetString("SpeedTest.TestFile.Ps-2"), "http://gst.prod.dl.playstation.net/gst/prod/00/PPSA04478_00/app/pkg/26/f_f2e4ff2bc3be11cb844dfe2a7ff8df357d7930152fb5984294a794823ec7472b/EP1464-PPSA04478_00-XXXXXXXXXXXXXXXX_0.pkg"),
        new SpeedTestFile("Ps-3", "Ps", ResourceHelper.GetString("SpeedTest.TestFile.Ps-3"), "http://gs2.ww.prod.dl.playstation.net/gs2/appkgo/prod/CUSA03962_00/4/f_526a2fab32d369a8ca6298b59686bf823fa9edfe95acb85bc140c27f810842ce/f/UP0102-CUSA03962_00-BH70000000000001_0.pkg"),
        new SpeedTestFile("Ps-4", "Ps", ResourceHelper.GetString("SpeedTest.TestFile.Ps-4"), "http://zeus.dl.playstation.net/cdn/UP1004/NPUB31154_00/eISFknCNDxqSsVVywSenkJdhzOIfZjrqKHcuGBHEGvUxQJksdPvRNYbIyWcxFsvH.pkg"),

        new SpeedTestFile("Akamai-Xbox", "Akamai", ResourceHelper.GetString("SpeedTest.TestFile.Akamai-Xbox"), "http://xvcf1.xboxlive.com/Z/routing/extraextralarge.txt"),
        new SpeedTestFile("Akamai-Ps", "Akamai", ResourceHelper.GetString("SpeedTest.TestFile.Akamai-Ps"), "http://gst.prod.dl.playstation.net/networktest/get_192m"),
        new SpeedTestFile("Akamai-Ns", "Akamai", ResourceHelper.GetString("SpeedTest.TestFile.Akamai-Ns"), "http://ctest-dl-lp1.cdn.nintendo.net/30m"),
        new SpeedTestFile("Akamai-Ea", "Akamai", ResourceHelper.GetString("SpeedTest.TestFile.Akamai-Ea"), "http://origin-a.akamaihd.net/EA-Desktop-Client-Download/installer-releases/EA%20app.dmg")
    ];
}