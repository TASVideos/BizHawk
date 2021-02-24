#nullable enable

using System;
using System.Collections.Generic;

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo
namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareDatabase
	{
		private static readonly Lazy<FirmwareDatabase> _lazy = new(() =>
		{
			static void AppleII(SystemBuilderScope sys)
			{
				sys.AddFirmware("AppleIIe", "AppleIIe.rom", fw =>
				{
					fw.AddOption("B8EA90ABE135A0031065E01697C4A3A20D51198B", 16384, "AppleIIe.rom", "Apple II e");
				});
				sys.AddFirmware("DiskII", "DiskII.rom", fw =>
				{
					fw.AddOption("D4181C9F046AAFC3FB326B381BAAC809D9E38D16", 256, "AppleIIe_DiskII.rom", "Disk II");
				});
			}
			static void Atari7800(SystemBuilderScope sys)
			{
				sys.AddFirmware("Bios_NTSC", "NTSC Bios", fw =>
				{
					fw.AddOption("D9D134BB6B36907C615A594CC7688F7BFCEF5B43", 4096, "A78_NTSC_bios.bin", fw.Description);
#if false
					fw.AddOption("CE236581AB7921B59DB95BA12837C22F160896CB", 4096, "A78_NTSC_speed_bios.bin", $"{fw.Description} speed");
#endif
				});
				sys.Add1OptionFirmware("Bios_PAL", "5A140136A16D1D83E4FF32A19409CA376A8DF874", 16384, "A78_PAL_BIOS.bin", "PAL Bios");
				sys.Add1OptionFirmware("Bios_HSC", "A3AF676991391A6DD716C79022D4947206B78164", 4096, "A78_highscore.bin", "Highscore Bios");
			}
			static void C64(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Kernal", "1D503E56DF85A62FEE696E7618DC5B4E781DF1BB", 8192, "C64_Kernal.bin", "Kernal Rom");
				sys.Add1OptionFirmware("Basic", "79015323128650C742A3694C9429AA91F355905E", 8192, "C64_Basic.bin", "Basic Rom");
				sys.Add1OptionFirmware("Chargen", "ADC7C31E18C7C7413D54802EF2F4193DA14711AA", 4096, "C64_Chargen.bin", "Chargen Rom");
				sys.Add1OptionFirmware("Drive1541", "AB16F56989B27D89BABE5F89C5A8CB3DA71A82F0", 16384, "C64_Drive-1541.bin", "1541 Disk Drive Rom");
				sys.Add1OptionFirmware("Drive1541II", "D3B78C3DBAC55F5199F33F3FE0036439811F7FB3", 16384, "C64_Drive-1541ii.bin", "1541-II Disk Drive Rom");
			}
			static void ChannelF(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("ChannelF_sl131253", "81193965A374D77B99B4743D317824B53C3E3C78", 1024, "ChannelF_SL31253.rom", "Channel F Rom0");
				sys.Add1OptionFirmware("ChannelF_sl131254", "8F70D1B74483BA3A37E86CF16C849D601A8C3D2C", 1024, "ChannelF_SL31254.rom", "Channel F Rom1");
			}
			static void Colecovision(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Bios", "45BEDC4CBDEAC66C7DF59E9E599195C778D86A92", 8192, "Coleco_Bios.bin", "Bios");
			}
			static void GameBoy(SystemBuilderScope sys)
			{
				sys.AddFirmware("World", "Game Boy Boot Rom", fw =>
				{
					fw.AddOption("4ED31EC6B0B175BB109C0EB5FD3D193DA823339F", 256, "dmg.bin", "Game Boy Boot Rom", FirmwareOptionStatus.Ideal);
					// Early revisions of GB/C boot ROMs are not well-supported because the corresponding CPU differences are not emulated.
					fw.AddOption("8BD501E31921E9601788316DBD3CE9833A97BCBC", 256, "dmg0.bin", "Game Boy Boot Rom (Early J Revision)", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("4E68F9DA03C310E84C523654B9026E51F26CE7F0", 256, "mgb.bin", "Game Boy Boot Rom (Pocket)", FirmwareOptionStatus.Acceptable);
				});
			}
			static void GBA(SystemBuilderScope sys)
			{
				sys.AddFirmware("Bios", "Bios", fw =>
				{
					fw.AddOption("300C20DF6731A33952DED8C436F7F186D25D3492", 16384, "GBA_bios.rom", $"{fw.Description} (World)");
					fw.AddOption("AA98A2AD32B86106340665D1222D7D973A1361C7", 16384, "GBA_bios_Debug-(J).rom", $"{fw.Description} (J Debug)");
				});
			}
			static void GBC(SystemBuilderScope sys)
			{
				sys.AddFirmware("World", "Game Boy Color Boot Rom", fw =>
				{
					fw.AddOption("1293D68BF9643BC4F36954C1E80E38F39864528D", 2304, "cgb.bin", "Game Boy Color Boot Rom", FirmwareOptionStatus.Ideal);
					fw.AddOption("DF5A0D2D49DE38FBD31CC2AAB8E62C8550E655C0", 2304, "cgb0.bin", "Game Boy Color Boot Rom (Early Revision)", FirmwareOptionStatus.Unacceptable);
				});
				sys.AddFirmware("AGB", "Game Boy Color Boot Rom (GBA)", fw =>
				{
					fw.AddOption("FA5287E24B0FA533B3B5EF2B28A81245346C1A0F", 2304, "agb.bin", "Game Boy Color Boot Rom (GBA)", FirmwareOptionStatus.Ideal);
					fw.AddOption("1ECAFA77AB3172193F3305486A857F443E28EBD9", 2304, "agb_gambatte.bin", "Game Boy Color Boot Rom (GBA, Gambatte RE)", FirmwareOptionStatus.Bad);
				});
			}
			static void Genesis(SystemBuilderScope sys)
			{
				sys.AddFirmware("CD_BIOS_EU", "Mega CD Bios (Europe)", fw =>
				{
					fw.AddOption("F891E0EA651E2232AF0C5C4CB46A0CAE2EE8F356", 131072, "MCD_eu_9210.bin", "Mega CD EU (9210)");
					fw.AddOption("7063192AE9F6B696C5B81BC8F0A9FE6F0C400E58", 131072, "MCD_eu_9303.bin", "Mega CD EU (9303)");
					fw.AddOption("523B3125FB0AC094E16AA072BC6CCDCA22E520E5", 131072, "MCD_eu_9306.bin", "Mega CD EU (9310)"); // ?? is this size correct?
				});
				sys.AddFirmware("CD_BIOS_JP", "Mega CD Bios (Japan)", fw =>
				{
					fw.AddOption("4846F448160059A7DA0215A5DF12CA160F26DD69", 131072, "MCD_jp_9111.bin", "Mega CD JP (9111)");
					fw.AddOption("E4193C6AE44C3CEA002707D2A88F1FBCCED664DE", 131072, "MCD_jp_9112.bin", "Mega CD JP (9112)");
				});
				sys.AddFirmware("CD_BIOS_US", "Sega CD Bios (USA)", fw =>
				{
					fw.AddOption("F4F315ADCEF9B8FEB0364C21AB7F0EAF5457F3ED", 131072, "SCD_us_9210.bin", "Sega CD US (9210)");
					fw.AddOption("BD3EE0C8AB732468748BF98953603CE772612704", 131072, "SCD_us_9303.bin", "Sega CD US (9303)");
				});
			}
			static void Intellivision(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("EROM", "5A65B922B562CB1F57DAB51B73151283F0E20C7A", 8192, "INTV_EROM.bin", "Executive Rom");
				sys.Add1OptionFirmware("GROM", "F9608BB4AD1CFE3640D02844C7AD8E0BCD974917", 2048, "INTV_GROM.bin", "Graphics Rom");
			}
			static void Lynx(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Boot", "E4ED47FAE31693E016B081C6BDA48DA5B70D7CCB", 512, "LYNX_boot.img", "Boot Rom");
			}
			static void MSX(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("bios_test", "B398CFCB94C9F7E808E0FECE54813CFDFB96F8D0", 16384, "MSX_bios.rom", "MSX BIOS");
				sys.Add1OptionFirmware("basic_test", "18559FA9C2D9E99A319550D809009ECDBA6D396E", 16384, "MSX_cbios_basic.rom", "MSX BASIC (C-BIOS v0.29a)");
				sys.Add1OptionFirmware("bios_test_ext", "2F997E8A57528518C82AB3693FDAE243DBBCC508", 32768, "MSX_cbios_main_msx1.rom", "MSX BIOS (C-BIOS v0.29a)");
				sys.Add1OptionFirmware("bios_pal", "E998F0C441F4F1800EF44E42CD1659150206CF79", 16384, "MSX_8020-20bios.rom", "MSX BIOS (Philips VG-8020)");
				sys.Add1OptionFirmware("bios_jp", "DF48902F5F12AF8867AE1A87F255145F0E5E0774", 16384, "MSX_4000bios.rom", "MSX BIOS (FS-4000)");
			}
			static void NDS(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("bios7", "24F67BDEA115A2C847C8813A262502EE1607B7DF", 16384, "NDS_Bios7.bin", "ARM7 BIOS"); // comment contained duplicate as id/filename: "NDS+Bios_Arm7"/"biosnds7.rom"
				sys.Add1OptionFirmware("bios9", "BFAAC75F101C135E32E2AAF541DE6B1BE4C8C62D", 4096, "NDS_Bios9.bin", "ARM9 BIOS"); // comment contained duplicate as id/filename: "NDS+Bios_Arm9"/"biosnds9.rom"
				sys.Add1OptionFirmware("firmware", "22A7547DBC302BCBFB4005CFB5A2D426D3F85AC6", 262144, "NDS_Firmware.bin", "NDS Firmware (note: given hash is with blank user data)");
			}
			static void NES(SystemBuilderScope sys)
			{
				sys.AddFirmware("Bios_FDS", desc: "Bios", fw =>
				{
					// FDS has two OK variants
					fw.AddOption("57FE1BDEE955BB48D357E463CCBF129496930B62", 8192, "FDS_disksys-nintendo.rom", $"{fw.Description} (Nintendo)", FirmwareOptionStatus.Ideal);
					fw.AddOption("E4E41472C454F928E53EB10E0509BF7D1146ECC1", 8192, "FDS_disksys-nintendo.rom", $"{fw.Description} (TwinFC)");

					// resources:
					// http://tcrf.net/Family_Computer_Disk_System
				});
			}
			static void Odyssey2(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("BIOS", "B2E1955D957A475DE2411770452EFF4EA19F4CEE", 1024, "O2_Odyssey2.bin", "Odyssey 2 Bios");
				sys.Add1OptionFirmware("BIOS-C52", "A6120AED50831C9C0D95DBDF707820F601D9452E", 1024, "O2_PhillipsC52.bin", "Phillips C52 Bios");
				sys.Add1OptionFirmware(new FirmwareID("G7400", "BIOS"), "5130243429B40B01A14E1304D0394B8459A6FBAE", 1024, "G7400_bios.bin", "G7400 Bios");
			}
			static void PCECD(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Bios", "79F5FF55DD10187C7FD7B8DAAB0B3FFBD1F56A2C", 262144, "PCECD_3.0-(J).pce", "Super CD Bios (J)");
				sys.Add1OptionFirmware("GE-Bios", "014881A959E045E00F4DB8F52955200865D40280", 32768, "PCECD_gecard.pce", "Games Express CD Card (Japan)");
			}
			static void PCFX(SystemBuilderScope sys)
			{
				sys.AddFirmware("BIOS", "PCFX bios", fw =>
				{
					fw.AddOption("1A77FD83E337F906AECAB27A1604DB064CF10074", 1024 * 1024, "PCFX_bios.bin", "PCFX BIOS 1.00", FirmwareOptionStatus.Ideal);
					fw.AddOption("8B662F7548078BE52A871565E19511CCCA28C5C8", 1024 * 1024, "PCFX_v101.bin", "PCFX BIOS 1.01", FirmwareOptionStatus.Acceptable);
				});
				sys.AddFirmware("SCSIROM", "fx-scsi.rom", fw =>
				{
					fw.AddOption("65482A23AC5C10A6095AEE1DB5824CCA54EAD6E5", 512 * 1024, "PCFX_fx-scsi.rom", "PCFX SCSI ROM");
				});
			}
			static void PS2(SystemBuilderScope sys)
			{
				sys.AddFirmware("BIOS", "PS2 Bios", fw =>
				{
					fw.AddOption("FBD54BFC020AF34008B317DCB80B812DD29B3759", 4 * 1024 * 1024, "ps2-0230j-20080220.bin", "PS2 Bios");
					fw.AddOption("8361D615CC895962E0F0838489337574DBDC9173", 4 * 1024 * 1024, "ps2-0220a-20060905.bin", "PS2 Bios");
					fw.AddOption("DA5AACEAD2FB55807D6D4E70B1F10F4FDCFD3281", 4 * 1024 * 1024, "ps2-0220e-20060905.bin", "PS2 Bios");
					fw.AddOption("3BAF847C1C217AA71AC6D298389C88EDB3DB32E2", 4 * 1024 * 1024, "ps2-0220j-20060905.bin", "PS2 Bios");
					fw.AddOption("F9229FE159D0353B9F0632F3FDC66819C9030458", 4 * 1024 * 1024, "ps2-0230a-20080220.bin", "PS2 Bios", FirmwareOptionStatus.Ideal);
					fw.AddOption("9915B5BA56798F4027AC1BD8D10ABE0C1C9C326A", 4 * 1024 * 1024, "ps2-0230e-20080220.bin", "PS2 Bios");
				});
			}
			static void PSX(SystemBuilderScope sys)
			{
				var ps_dtl_h2000 = sys.AddFile("1A8D6F9453111B1D317BB7DAE300495FBF54600C", 524288, "PSX_DTLH2000.bin", "DTL-H2000 Devkit [t]"); // not really sure what to do with this one, let's just call it region-free

				// in general, alternates aren't allowed.. their quality isn't known.
				// we have this comment from fobby.net: "SCPH7502 works fine for European games" (TBD)
				// however, we're sticking with the 3.0 series.
				// please note: 2.1 or 2.2 would be a better choice, as the dates are the same and the bioses are more likely to matching in terms of entry points and such.
				// but 3.0 is what mednafen used
				sys.AddFirmware("U", "BIOS (U)", fw =>
				{
					fw.AddOption("0555C6FAE8906F3F09BAF5988F00E55F88E9F30B", 524288, "PSX_3.0(A).bin", "PSX BIOS (Version 3.0 11/18/96 A)", "Used on SCPH-5501, SCPH-5503, SCPH-7003 [g]. This is for Rev C hardware [w]. Recommended for (U) [f].");
					fw.AddOption("649895EFD79D14790EABB362E94EB0622093DFB9", 524288, "PSX_2.0(A).bin", "PSX BIOS (Version 2.0 05/07/95 A)", "Used on DTL-H1001 [g]. This is for Rev B hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("CA7AF30B50D9756CBD764640126C454CFF658479", 524288, "PSX_2.1(A).bin", "PSX BIOS (Version 2.1 07/17/95 A)", "Used on DTL-H1101 [g]. This is for Rev B hardware, presumably.", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("10155D8D6E6E832D6EA66DB9BC098321FB5E8EBF", 524288, "PSX_2.2(A).bin", "PSX BIOS (Version 2.2 12/04/95 A)", "Used on SCPH-1001, DTL-H1201, DTL-H3001 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("14DF4F6C1E367CE097C11DEAE21566B4FE5647A9", 524288, "PSX_4.1(A).bin", "PSX BIOS (Version 4.1 12/16/97 A)", "Used on SCPH-7001, SCPH-7501, SCPH-7503, SCPH-9001, SCPH-9003, SCPH-9903 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("DCFFE16BD90A723499AD46C641424981338D8378", 524288, "PSX_4.5(A).bin", "PSX BIOS (Version 4.5 05/25/00 A)", "Used on PSone SCPH-101 [g]. This is for Rev C PSone hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("1B0DBDB23DA9DC0776AAC58D0755DC80FEA20975", 524288, "PSX_4.1(A).bin", "PSX BIOS (Version 4.1 11/14/97 A)", "Used on SCPH-7000W [t].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("C40146361EB8CF670B19FDC9759190257803CAB7", 524288, "PSX_rom.bin", "PSX BIOS (Version 5.0 06/23/03 A)", "Found on a PS3. [t]", FirmwareOptionStatus.Unacceptable);
					fw.AddOption(in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);
				});
				sys.AddFirmware("J", "BIOS (J)", fw =>
				{
					fw.AddOption("B05DEF971D8EC59F346F2D9AC21FB742E3EB6917", 524288, "PSX_3.0(J).bin", "PSX BIOS (Version 3.0 09/09/96 J)", "Used on SCPH-5500 [g]. This is for Rev C hardware [w]. Recommended for (J) [f].");
					fw.AddOption("343883A7B555646DA8CEE54AADD2795B6E7DD070", 524288, "PSX_1.0(J).bin", "PSX BIOS (Version 1.0 J)", "Used on SCPH-1000, DTL-H1000 [g]. This is Rev for A hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("B06F4A861F74270BE819AA2A07DB8D0563A7CC4E", 524288, "PSX_1.1(J).bin", "PSX BIOS (Version 1.1 01/22/95)", "Used on SCPH-3000, DTL-H1000H [g]. This is for Rev B hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("E38466A4BA8005FBA7E9E3C7B9EFEBA7205BEE3F", 524288, "PSX_2.1(J).bin", "PSX BIOS (Version 2.1 07/17/95 J)", "Used on SCPH-3500 [g]. This is for Rev B hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("FFA7F9A7FB19D773A0C3985A541C8E5623D2C30D", 524288, "PSX_2.2(J).bin", "PSX BIOS (Version 2.2 12/04/95 J)", "Used on SCPH-5000, DTL-H1200, DTL-H3000 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("E340DB2696274DDA5FDC25E434A914DB71E8B02B", 524288, "PSX_2.2(J)-bad.bin", "PSX BIOS (Version 2.2 12/04/95 J)", "BAD DUMP OF SCPH-5000. Found on [p].", isBad: true, FirmwareOptionStatus.Bad);
					fw.AddOption("81622ACE63E25696A5D884692E554D350DDF57A6", 526083, "PSX_2.2(J)-bad2.bin", "PSX BIOS (Version 2.2 12/04/95 J)", "BAD DUMP OF SCPH-5000.", isBad: true, FirmwareOptionStatus.Bad);
					fw.AddOption("73107D468FC7CB1D2C5B18B269715DD889ECEF06", 524288, "PSX_2.2(D).bin", "PSX BIOS (Version 2.2 03/06/96 D)", "Used on DTL-H1100 [g]. This is for Rev C hardware, presumably.", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("77B10118D21AC7FFA9B35F9C4FD814DA240EB3E9", 524288, "PSX_4.0(J).bin", "PSX BIOS (Version 4.0 08/18/97 J)", "Used on SCPH-7000, SCPH-7500, SCPH-9000 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("339A48F4FCF63E10B5B867B8C93CFD40945FAF6C", 524288, "PSX_4.3(J).bin", "PSX BIOS (Version 4.3 03/11/00 J)", "Used on PSone SCPH-100 [g]. This is for Rev C PSone hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("D7D6BE084F51354BC951D8FA2D8D912AA70ABC5E", 4194304, "PSX_5.0(J).bin", "PSX BIOS (Version 5.0 10/27/00 J)", "Found on a PS2 [p]. May be known as SCPH18000.BIN.", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("15C94DA3CC5A38A582429575AF4198C487FE893C", 1048576, "PSX_2.2(J).bin", "PSX BIOS (Version 2.2 12/04/95 J)", "Used on SCPH-5903 [t].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption(in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);
				});
				sys.AddFirmware("E", "BIOS (E)", fw =>
				{
					fw.AddOption("F6BC2D1F5EB6593DE7D089C425AC681D6FFFD3F0", 524288, "PSX_3.0(E).bin", "PSX BIOS (Version 3.0 01/06/97 E)", "Used on SCPH-5502, SCPH-5552 [g]. This is for Rev C hardware [w]. Recommended for (E) [f].");
					fw.AddOption("20B98F3D80F11CBF5A7BFD0779B0E63760ECC62C", 524288, "PSX_2.0(E).bin", "PSX BIOS (Version 2.0 05/10/95 E)", "Used on DTL-H1002, SCPH-1002 [g]. This is for Rev B hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("76CF6B1B2A7C571A6AD07F2BAC0DB6CD8F71E2CC", 524288, "PSX_2.1(E).bin", "PSX BIOS (Version 2.1 07/17/95 E)", "Used on SCPH-1002, DTL-H1102 [g]. This is for Rev B hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("B6A11579CAEF3875504FCF3831B8E3922746DF2C", 524288, "PSX_2.2(E).bin", "PSX BIOS (Version 2.2 12/04/95 E)", "Used on SCPH-1002, DTL-H1202, DTL-H3002 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("F8DE9325FC36FCFA4B29124D291C9251094F2E54", 524288, "PSX_3.0(E)-bad.bin", "PSX BIOS (Version 3.0 01/06/97 E)", "BAD DUMP OF SCPH-5502. Found on [p].", isBad: true, FirmwareOptionStatus.Bad);
					fw.AddOption("8D5DE56A79954F29E9006929BA3FED9B6A418C1D", 524288, "PSX_4.1(E).bin", "PSX BIOS (Version 4.1 12/16/97 E)", "Used on SCPH-7002, SCPH-7502, SCPH-9002 [g]. This is for Rev C hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("BEB0AC693C0DC26DAF5665B3314DB81480FA5C7C", 524288, "PSX_4.4(E).bin", "PSX BIOS (Version 4.4 03/24/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption("DBC7339E5D85827C095764FC077B41F78FD2ECAE", 524288, "PSX_4.5(E).bin", "PSX BIOS (Version 4.5 05/25/00 E)", "Used on PSone SCPH-102 [g]. This is for Rev C PSone hardware [w].", FirmwareOptionStatus.Unacceptable);
					fw.AddOption(in ps_dtl_h2000, FirmwareOptionStatus.Unacceptable);
				});

				// resources:
				// http://forum.fobby.net/index.php?t=msg&goto=2763 [f]
				// http://www.psxdev.net/forum/viewtopic.php?f=69&t=56 [p]
				// https://en.wikipedia.org/wiki/PlayStation_models#Comparison_of_models [w]
				// https://github.com/petrockblog/RetroPie-Setup/wiki/PCSX-Core-Playstation-1 [g]
				// http://redump.org/datfile/psx-bios/ also
				// http://emulation.gametechwiki.com/index.php/File_Hashes [t]
			}
			static void Saturn(SystemBuilderScope sys)
			{
				var ss_100_j = sys.AddFile("2B8CB4F87580683EB4D760E4ED210813D667F0A2", 524288, "SAT_1.00-(J).bin", "Bios v1.00 (J)");
				var ss_100_ue = sys.AddFile("FAA8EA183A6D7BBE5D4E03BB1332519800D3FBC3", 524288, "SAT_1.00-(U+E).bin", "Bios v1.00 (U+E)");
				var ss_100a_ue = sys.AddFile("3BB41FEB82838AB9A35601AC666DE5AACFD17A58", 524288, "SAT_1.00a-(U+E).bin", "Bios v1.00a (U+E)"); // ?? is this size correct?
				var ss_101_j = sys.AddFile("DF94C5B4D47EB3CC404D88B33A8FDA237EAF4720", 524288, "SAT_1.01-(J).bin", "Bios v1.01 (J)"); // ?? is this size correct?
				// for saturn, we think any bios region can pretty much run any iso
				// so, we're going to lay this out carefully so that we choose things in a sensible order, but prefer the correct region
				sys.AddFirmware("J", "Bios (J)", fw =>
				{
					fw.AddOption(in ss_100_j);
					fw.AddOption(in ss_101_j);
					fw.AddOption(in ss_100_ue);
					fw.AddOption(in ss_100a_ue);
				});
				sys.AddFirmware("U", "Bios (U)", fw =>
				{
					fw.AddOption(in ss_100_ue);
					fw.AddOption(in ss_100a_ue);
					fw.AddOption(in ss_100_j);
					fw.AddOption(in ss_101_j);
				});
				sys.AddFirmware("E", "Bios (E)", fw =>
				{
					fw.AddOption(in ss_100_ue);
					fw.AddOption(in ss_100a_ue);
					fw.AddOption(in ss_100_j);
					fw.AddOption(in ss_101_j);
				});

				sys.Add1OptionFirmware("KOF95", "A67CD4F550751F8B91DE2B8B74528AB4E0C11C77", 2 * 1024 * 1024, "SAT_KoF95.bin", "King of Fighters cartridge");
				sys.Add1OptionFirmware("ULTRAMAN", "56C1B93DA6B660BF393FBF48CA47569000EF4047", 2 * 1024 * 1024, "SAT_Ultraman.bin", "Ultraman cartridge");
			}
			static void Sega32X(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("G", "DBEBD76A448447CB6E524AC3CB0FD19FC065D944", 256, "32X_G_BIOS.BIN", "32x 68k BIOS");
				sys.Add1OptionFirmware("M", "1E5B0B2441A4979B6966D942B20CC76C413B8C5E", 2048, "32X_M_BIOS.BIN", "32x SH2 MASTER BIOS");
				sys.Add1OptionFirmware("S", "4103668C1BBD66C5E24558E73D4F3F92061A109A", 1024, "32X_S_BIOS.BIN", "32x SH2 SLAVE BIOS");
			}
			static void SMS(SystemBuilderScope sys)
			{
				sys.AddFirmware("Export", "SMS Bios (USA/Export)", fw =>
				{
					fw.AddOption("C315672807D8DDB8D91443729405C766DD95CAE7", 8192, "SMS_us_1.3.sms", "SMS BIOS 1.3 (USA, Europe)");
					fw.AddOption("29091FF60EF4C22B1EE17AA21E0E75BAC6B36474", 8192, "SMS_us_1.0b.sms", "SMS BIOS 1.0 (USA) (Proto)"); // ?? is this size correct?
					fw.AddOption("4A06C8E66261611DCE0305217C42138B71331701", 8192, "SMS_m404.sms", "SMS BIOS (USA) (M404) (Proto)"); // ?? is this size correct?
				});
				sys.AddFirmware("Japan", "SMS Bios (Japan)", fw =>
				{
					fw.AddOption("A8C1B39A2E41137835EDA6A5DE6D46DD9FADBAF2", 8192, "SMS_jp_2.1.sms", "SMS BIOS 2.1 (Japan)");
				});
				sys.AddFirmware("Korea", "SMS Bios (Korea)", fw =>
				{
					fw.AddOption("2FEAFD8F1C40FDF1BD5668F8C5C02E5560945B17", 131072, "SMS_kr.sms", "SMS BIOS (Kr)"); // ?? is this size correct?
				});
			}
			static void SNES(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Rom_SGB", "973E10840DB683CF3FAF61BD443090786B3A9F04", 262144, "SNES_sgb.sfc", "Super GameBoy Rom"); // World (Rev B) ?
				sys.Add1OptionFirmware("CX4", "A002F4EFBA42775A31185D443F3ED1790B0E949A", 3072, "SNES_cx4.rom", "CX4 Rom");
				sys.Add1OptionFirmware("DSP1", "188D471FEFEA71EB53F0EE7064697FF0971B1014", 8192, "SNES_dsp1.rom", "DSP1 Rom");
				sys.Add1OptionFirmware("DSP1b", "78B724811F5F18D8C67669D9390397EB1A47A5E2", 8192, "SNES_dsp1b.rom", "DSP1b Rom");
				sys.Add1OptionFirmware("DSP2", "198C4B1C3BFC6D69E734C5957AF3DBFA26238DFB", 8192, "SNES_dsp2.rom", "DSP2 Rom");
				sys.Add1OptionFirmware("DSP3", "558DA7CB3BD3876A6CA693661FFC6C110E948CF9", 8192, "SNES_dsp3.rom", "DSP3 Rom");
				sys.Add1OptionFirmware("DSP4", "AF6478AECB6F1B67177E79C82CA04C56250A8C72", 8192, "SNES_dsp4.rom", "DSP4 Rom");
				sys.Add1OptionFirmware("ST010", "6472828403DE3589433A906E2C3F3D274C0FF008", 53248, "SNES_st010.rom", "ST010 Rom");
				sys.Add1OptionFirmware("ST011", "FECBAE2CEC76C710422486BAA186FFA7CA1CF925", 53248, "SNES_st011.rom", "ST011 Rom");
				sys.Add1OptionFirmware("ST018", "91383B92745CC7CC4F15409AC5BC2C2F699A43F1", 163840, "SNES_st018.rom", "ST018 Rom");
			}
			static void TI83(SystemBuilderScope sys)
			{
				sys.AddFirmware("Rom", "TI-83 Rom", fw =>
				{
					fw.AddOption("CE08F6A808701FC6672230A790167EE485157561", 262144, "TI83_102.rom", "TI-83 Rom v1.02"); // ?? is this size correct?
					fw.AddOption("8399E384804D8D29866CAA4C8763D7A61946A467", 262144, "TI83_103.rom", "TI-83 Rom v1.03"); // ?? is this size correct?
					fw.AddOption("33877FF637DC5F4C5388799FD7E2159B48E72893", 262144, "TI83_104.rom", "TI-83 Rom v1.04"); // ?? is this size correct?
					fw.AddOption("3D65C2A1B771CE8E5E5A0476EC1AA9C9CDC0E833", 262144, "TI83_106.rom", "TI-83 Rom v1.06"); // ?? is this size correct?
					fw.AddOption("EF66DAD3E7B2B6A86F326765E7DFD7D1A308AD8F", 262144, "TI83_107.rom", "TI-83 Rom v1.07"); // formerly the 1.?? recommended one
					fw.AddOption("9C74F0B61655E9E160E92164DB472AD7EE02B0F8", 262144, "TI83_108.rom", "TI-83 Rom v1.08"); // ?? is this size correct?
					fw.AddOption("37EAEEB9FB5C18FB494E322B75070E80CC4D858E", 262144, "TI83p_103b.rom", "TI-83 Plus Rom v1.03"); // ?? is this size correct?
					fw.AddOption("6615DF5554076B6B81BD128BF847D2FF046E556B", 262144, "TI83p_112.rom", "TI-83 Plus Rom v1.12"); // ?? is this size correct?
				});
			}
			static void Vectrex(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("Bios", "B9BBF5BB0EAC52D039A4A993A2D8064B862C9E28", 4096, "Vectrex_Bios.bin", "Bios");
				sys.Add1OptionFirmware("Minestorm", "65D07426B520DDD3115D40F255511E0FD2E20AE7", 8192, "Vectrex_Minestorm.vec", "Game");
			}
			static void ZXSpectrum(SystemBuilderScope sys)
			{
				sys.Add1OptionFirmware("PentagonROM", "A584272F21DC82C14B7D4F1ED440E23A976E71F0", 32768, "ZX_pentagon.rom", "Russian Pentagon Clone ROM");
				sys.Add1OptionFirmware("TRDOSROM", "282EB7BC819AAD2A12FD954E76F7838A4E1A7929", 16384, "ZX_trdos.rom", "TRDOS ROM");
			}
			DBBuilder builder = new();
			builder.AddSystem("NES", NES);
			builder.AddSystem("SNES", SNES);
			builder.AddSystem("PCECD", PCECD);
			builder.AddSystem("A78", Atari7800);
			builder.AddSystem("Coleco", Colecovision);
			builder.AddSystem("Vectrex", Vectrex);
			builder.AddSystem("GBA", GBA);
			builder.AddSystem("NDS", NDS);
			builder.AddSystem("Lynx", Lynx);
			builder.AddSystem("INTV", Intellivision);
			builder.AddSystem("C64", C64);
			builder.AddSystem("ZXSpectrum", ZXSpectrum);
			builder.AddSystem("MSX", MSX);
			builder.AddSystem("ChannelF", ChannelF);
			builder.AddSystem("SAT", Saturn);
			builder.AddSystem("TI83", TI83);
			builder.AddSystem("GEN", Genesis);
			builder.AddSystem("32X", Sega32X);
			builder.AddSystem("SMS", SMS);
			builder.AddSystem("PSX", PSX);
			builder.AddSystem("AppleII", AppleII);
			builder.AddSystem("O2", Odyssey2);
			builder.AddSystem("GB", GameBoy);
			builder.AddSystem("GBC", GBC);
			builder.AddSystem("PCFX", PCFX);
			builder.AddSystem("PS2", PS2);
			return builder.Build();
		});

		public static FirmwareDatabase Instance => _lazy.Value;

		public IEnumerable<FirmwareFile> FirmwareFiles => FirmwareFilesByHash.Values;

		public readonly IReadOnlyDictionary<string, FirmwareFile> FirmwareFilesByHash;

		public readonly IReadOnlyCollection<FirmwareOption> FirmwareOptions;

		public readonly IReadOnlyCollection<FirmwareRecord> FirmwareRecords;

		public FirmwareDatabase(
			IReadOnlyDictionary<string, FirmwareFile> filesByHash,
			IReadOnlyCollection<FirmwareOption> options,
			IReadOnlyCollection<FirmwareRecord> records)
		{
			FirmwareFilesByHash = filesByHash;
			FirmwareOptions = options;
			FirmwareRecords = records;
		}

		private sealed class DBBuilder
		{
			private readonly List<SystemBuilderScope> _builders = new();

			private readonly Dictionary<string, FirmwareFile> _filesByHash = new();

			private bool _isBuilt = false;

			private readonly List<FirmwareOption> _options = new();

			private readonly List<FirmwareRecord> _records = new();

			public FirmwareFile AddFile(
				string hash,
				long size,
				string recommendedName,
				string desc,
				string additionalInfo = "",
				bool isBad = false)
					=> _filesByHash[hash] = new(
						hash: hash,
						size: size,
						recommendedName: recommendedName,
						desc: desc,
						additionalInfo: additionalInfo,
						isBad: isBad);

			public void AddSystem(string systemID, Action<SystemBuilderScope> buildAction)
			{
				SystemBuilderScope builder = new(this, systemID);
				buildAction(builder);
				_builders.Add(builder);
			}

			public FirmwareDatabase Build()
			{
				const string DOUBLE_BUILD_ERROR_MSG = "double invocation of " + nameof(DBBuilder) + "." + nameof(Build);
				if (_isBuilt) throw new InvalidOperationException(DOUBLE_BUILD_ERROR_MSG);
				_isBuilt = true;
				void AddFF(FirmwareFile ff) => _filesByHash[ff.Hash] = ff;
				foreach (var builder in _builders) builder.Build(AddFF, _options.Add, _records.Add);
				return new(_filesByHash, _options, _records);
			}
		}

		private sealed class FirmwareBuilderScope
		{
			private readonly List<FirmwareFile> _files = new();

			private bool _isBuilt = false;

			private readonly List<FirmwareOption> _options = new();

			public readonly string Description;

			public readonly FirmwareID ID;

			public FirmwareBuilderScope(string desc, FirmwareID id)
			{
				Description = desc;
				ID = id;
			}

			/// <remarks>assuming <paramref name="ff"/> was created with <see cref="DBBuilder.AddFile"/></remarks>
			public void AddOption(in FirmwareFile ff, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
				=> _options.Add(new FirmwareOption(ID, ff.Hash, ff.Size, ff.IsBad ? FirmwareOptionStatus.Bad : status));

			public void AddOption(
				string hash,
				long size,
				string recommendedName,
				string desc,
				string additionalInfo,
				bool isBad,
				FirmwareOptionStatus status)
			{
				FirmwareFile ff = new(
					hash: hash,
					size: size,
					recommendedName: recommendedName,
					desc: desc,
					additionalInfo: additionalInfo,
					isBad: isBad);
				AddOption(ff, status);
				_files.Add(ff);
			}

			public void AddOption(
				string hash,
				long size,
				string recommendedName,
				string desc,
				string additionalInfo,
				FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				FirmwareFile ff = new(
					hash: hash,
					size: size,
					recommendedName: recommendedName,
					desc: desc,
					additionalInfo: additionalInfo);
				AddOption(ff, status);
				_files.Add(ff);
			}

			public void AddOption(
				string hash,
				long size,
				string recommendedName,
				string desc,
				FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				FirmwareFile ff = new(hash: hash, size: size, recommendedName: recommendedName, desc: desc);
				AddOption(ff, status);
				_files.Add(ff);
			}

			public FirmwareRecord Build(Action<FirmwareFile> addFile, Action<FirmwareOption> addOption)
			{
				const string DOUBLE_BUILD_ERROR_MSG = "double invocation of " + nameof(FirmwareBuilderScope) + "." + nameof(Build);
				if (_isBuilt) throw new InvalidOperationException(DOUBLE_BUILD_ERROR_MSG);
				_isBuilt = true;
				foreach (var ff in _files) addFile(ff);
				foreach (var fo in _options) addOption(fo);
				return new(ID, Description);
			}
		}

		private sealed class SystemBuilderScope
		{
			private readonly List<FirmwareBuilderScope> _builders = new();

			private bool _isBuilt = false;

			private readonly DBBuilder _rootBuilder;

			private readonly string _systemID;

			public SystemBuilderScope(DBBuilder rootBuilder, string systemID)
			{
				_rootBuilder = rootBuilder;
				_systemID = systemID;
			}

			public FirmwareFile AddFile(
				string hash,
				long size,
				string recommendedName,
				string desc,
				string additionalInfo = "",
				bool isBad = false)
					=> _rootBuilder.AddFile(
						hash: hash,
						size: size,
						recommendedName: recommendedName,
						desc: desc,
						additionalInfo: additionalInfo,
						isBad: isBad);

			public void AddFirmware(string id, string desc, Action<FirmwareBuilderScope> buildAction)
			{
				FirmwareBuilderScope builder = new(desc, new(_systemID, id));
				buildAction(builder);
				_builders.Add(builder);
			}

			/// <remarks>assuming <paramref name="ff"/> was created with <see cref="DBBuilder.AddFile"/></remarks>
			public void Add1OptionFirmware(string id, in FirmwareFile ff, FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				FirmwareBuilderScope builder = new(ff.Description, new(_systemID, id));
				builder.AddOption(in ff, status);
				_builders.Add(builder);
			}

			/// <remarks>only used by G7400 (is it distinct enough from the Odyssey2 to warrant its own system ID? --yoshi)</remarks>
			public void Add1OptionFirmware(
				FirmwareID id,
				string hash,
				long size,
				string recommendedName,
				string desc,
				FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				FirmwareBuilderScope builder = new(desc, id);
				// this AddOption overload ensures a FirmwareFile will be added to the final list when built
				builder.AddOption(
					hash: hash,
					size: size,
					recommendedName: recommendedName,
					desc: desc,
					status: status);
				_builders.Add(builder);
			}

			public void Add1OptionFirmware(
				string id,
				string hash,
				long size,
				string recommendedName,
				string desc,
				FirmwareOptionStatus status = FirmwareOptionStatus.Acceptable)
			{
				FirmwareBuilderScope builder = new(desc, new(_systemID, id));
				// this AddOption overload ensures a FirmwareFile will be added to the final list when built
				builder.AddOption(
					hash: hash,
					size: size,
					recommendedName: recommendedName,
					desc: desc,
					status: status);
				_builders.Add(builder);
			}

			public void Build(Action<FirmwareFile> addFile, Action<FirmwareOption> addOption, Action<FirmwareRecord> addRecord)
			{
				const string DOUBLE_BUILD_ERROR_MSG = "double invocation of " + nameof(SystemBuilderScope) + "." + nameof(Build);
				if (_isBuilt) throw new InvalidOperationException(DOUBLE_BUILD_ERROR_MSG);
				_isBuilt = true;
				foreach (var builder in _builders) addRecord(builder.Build(addFile, addOption));
			}
		}
	}
}
