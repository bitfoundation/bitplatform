namespace Bit.BlazorUI;

/// <summary>
/// The table of countries the flag images and the country lookups of the library are built on.
/// </summary>
/// <remarks>
/// Every country is a shared <see cref="BitCountry"/> instance, named after itself so it can be
/// written straight into markup, and <see cref="All"/> is all of them in alphabetical order. The
/// table carries exactly the countries the packaged flag images cover, which is what lets
/// <see cref="HasFlag"/> answer for an image without asking the network for it.
/// <br />
/// The Find methods resolve any of the four ways a country is written down - the ISO 3166-1 alpha-2
/// and alpha-3 codes, the English name and the dialing code - back to its instance, out of a
/// dictionary rather than by scanning the table. A name is read as a page actually writes one rather
/// than as an exact key: the alternative names and the abbreviations a country is as widely known by
/// are answered too ("Czechia", "Türkiye", "Holland", "UK", "USA"), and so are the accents, the
/// punctuation and the spacing a second source spells it with ("Curaçao", "Guinea-Bissau").
/// <br />
/// Nothing is built before it is used: the countries are created the first time any of them is read, in one pass
/// over the table, and the lookups of the Find methods the first time one of them is called.
/// </remarks>
public class BitCountries
{
    public static BitCountry Afghanistan => Table.All[0]; // AF
    public static BitCountry AlandIslands => Table.All[1]; // AX
    public static BitCountry Albania => Table.All[2]; // AL
    public static BitCountry Algeria => Table.All[3]; // DZ
    public static BitCountry AmericanSamoa => Table.All[4]; // AS
    public static BitCountry Andorra => Table.All[5]; // AD
    public static BitCountry Angola => Table.All[6]; // AO
    public static BitCountry Anguilla => Table.All[7]; // AI
    public static BitCountry Antarctica => Table.All[8]; // AQ
    public static BitCountry AntiguaAndBarbuda => Table.All[9]; // AG
    public static BitCountry Argentina => Table.All[10]; // AR
    public static BitCountry Armenia => Table.All[11]; // AM
    public static BitCountry Aruba => Table.All[12]; // AW
    public static BitCountry Australia => Table.All[13]; // AU
    public static BitCountry Austria => Table.All[14]; // AT
    public static BitCountry Azerbaijan => Table.All[15]; // AZ
    public static BitCountry Bahamas => Table.All[16]; // BS
    public static BitCountry Bahrain => Table.All[17]; // BH
    public static BitCountry Bangladesh => Table.All[18]; // BD
    public static BitCountry Barbados => Table.All[19]; // BB
    public static BitCountry Belarus => Table.All[20]; // BY
    public static BitCountry Belgium => Table.All[21]; // BE
    public static BitCountry Belize => Table.All[22]; // BZ
    public static BitCountry Benin => Table.All[23]; // BJ
    public static BitCountry Bermuda => Table.All[24]; // BM
    public static BitCountry Bhutan => Table.All[25]; // BT
    public static BitCountry Bolivia => Table.All[26]; // BO
    public static BitCountry BosniaAndHerzegovina => Table.All[27]; // BA
    public static BitCountry Botswana => Table.All[28]; // BW
    public static BitCountry Brazil => Table.All[29]; // BR
    //public static BitCountry BritishIndianOceanTerritory; // IO, no flag image ships for it
    public static BitCountry BritishVirginIslands => Table.All[30]; // VG
    public static BitCountry Brunei => Table.All[31]; // BN
    public static BitCountry Bulgaria => Table.All[32]; // BG
    public static BitCountry BurkinaFaso => Table.All[33]; // BF
    public static BitCountry Burundi => Table.All[34]; // BI
    public static BitCountry Cambodia => Table.All[35]; // KH
    public static BitCountry Cameroon => Table.All[36]; // CM
    public static BitCountry Canada => Table.All[37]; // CA
    public static BitCountry CapeVerde => Table.All[38]; // CV
    public static BitCountry CaymanIslands => Table.All[39]; // KY
    public static BitCountry CentralAfricanRepublic => Table.All[40]; // CF
    public static BitCountry Chad => Table.All[41]; // TD
    public static BitCountry Chile => Table.All[42]; // CL
    public static BitCountry China => Table.All[43]; // CN
    public static BitCountry ChristmasIsland => Table.All[44]; // CX
    public static BitCountry CocosIslands => Table.All[45]; // CC
    public static BitCountry Colombia => Table.All[46]; // CO
    public static BitCountry Comoros => Table.All[47]; // KM
    public static BitCountry CookIslands => Table.All[48]; // CK
    public static BitCountry CostaRica => Table.All[49]; // CR
    public static BitCountry Croatia => Table.All[50]; // HR
    public static BitCountry Cuba => Table.All[51]; // CU
    public static BitCountry Curacao => Table.All[52]; // CW
    public static BitCountry Cyprus => Table.All[53]; // CY
    public static BitCountry CzechRepublic => Table.All[54]; // CZ
    public static BitCountry DemocraticRepublicOfTheCongo => Table.All[55]; // CD
    public static BitCountry Denmark => Table.All[56]; // DK
    public static BitCountry Djibouti => Table.All[57]; // DJ
    public static BitCountry Dominica => Table.All[58]; // DM
    public static BitCountry DominicanRepublic => Table.All[59]; // DO
    public static BitCountry EastTimor => Table.All[60]; // TL
    public static BitCountry Ecuador => Table.All[61]; // EC
    public static BitCountry Egypt => Table.All[62]; // EG
    public static BitCountry ElSalvador => Table.All[63]; // SV
    public static BitCountry EquatorialGuinea => Table.All[64]; // GQ
    public static BitCountry Eritrea => Table.All[65]; // ER
    public static BitCountry Estonia => Table.All[66]; // EE
    public static BitCountry Ethiopia => Table.All[67]; // ET
    public static BitCountry FalklandIslands => Table.All[68]; // FK
    public static BitCountry FaroeIslands => Table.All[69]; // FO
    public static BitCountry Fiji => Table.All[70]; // FJ
    public static BitCountry Finland => Table.All[71]; // FI
    public static BitCountry France => Table.All[72]; // FR
    public static BitCountry FrenchPolynesia => Table.All[73]; // PF
    public static BitCountry FrenchSouthernTerritories => Table.All[74]; // TF
    public static BitCountry Gabon => Table.All[75]; // GA
    public static BitCountry Gambia => Table.All[76]; // GM
    public static BitCountry Georgia => Table.All[77]; // GE
    public static BitCountry Germany => Table.All[78]; // DE
    public static BitCountry Ghana => Table.All[79]; // GH
    public static BitCountry Gibraltar => Table.All[80]; // GI
    public static BitCountry Greece => Table.All[81]; // GR
    public static BitCountry Greenland => Table.All[82]; // GL
    public static BitCountry Grenada => Table.All[83]; // GD
    public static BitCountry Guam => Table.All[84]; // GU
    public static BitCountry Guatemala => Table.All[85]; // GT
    public static BitCountry Guernsey => Table.All[86]; // GG
    public static BitCountry Guinea => Table.All[87]; // GN
    public static BitCountry GuineaBissau => Table.All[88]; // GW
    public static BitCountry Guyana => Table.All[89]; // GY
    public static BitCountry Haiti => Table.All[90]; // HT
    public static BitCountry Honduras => Table.All[91]; // HN
    public static BitCountry HongKong => Table.All[92]; // HK
    public static BitCountry Hungary => Table.All[93]; // HU
    public static BitCountry Iceland => Table.All[94]; // IS
    public static BitCountry India => Table.All[95]; // IN
    public static BitCountry Indonesia => Table.All[96]; // ID
    public static BitCountry Iran => Table.All[97]; // IR
    public static BitCountry Iraq => Table.All[98]; // IQ
    public static BitCountry Ireland => Table.All[99]; // IE
    public static BitCountry IsleOfMan => Table.All[100]; // IM
    public static BitCountry Israel => Table.All[101]; // IL
    public static BitCountry Italy => Table.All[102]; // IT
    public static BitCountry IvoryCoast => Table.All[103]; // CI
    public static BitCountry Jamaica => Table.All[104]; // JM
    public static BitCountry Japan => Table.All[105]; // JP
    public static BitCountry Jersey => Table.All[106]; // JE
    public static BitCountry Jordan => Table.All[107]; // JO
    public static BitCountry Kazakhstan => Table.All[108]; // KZ
    public static BitCountry Kenya => Table.All[109]; // KE
    public static BitCountry Kiribati => Table.All[110]; // KI
    //public static BitCountry Kosovo; // XK, no flag image ships for it
    public static BitCountry Kuwait => Table.All[111]; // KW
    public static BitCountry Kyrgyzstan => Table.All[112]; // KG
    public static BitCountry Laos => Table.All[113]; // LA
    public static BitCountry Latvia => Table.All[114]; // LV
    public static BitCountry Lebanon => Table.All[115]; // LB
    public static BitCountry Lesotho => Table.All[116]; // LS
    public static BitCountry Liberia => Table.All[117]; // LR
    public static BitCountry Libya => Table.All[118]; // LY
    public static BitCountry Liechtenstein => Table.All[119]; // LI
    public static BitCountry Lithuania => Table.All[120]; // LT
    public static BitCountry Luxembourg => Table.All[121]; // LU
    public static BitCountry Macau => Table.All[122]; // MO
    public static BitCountry Macedonia => Table.All[123]; // MK
    public static BitCountry Madagascar => Table.All[124]; // MG
    public static BitCountry Malawi => Table.All[125]; // MW
    public static BitCountry Malaysia => Table.All[126]; // MY
    public static BitCountry Maldives => Table.All[127]; // MV
    public static BitCountry Mali => Table.All[128]; // ML
    public static BitCountry Malta => Table.All[129]; // MT
    public static BitCountry MarshallIslands => Table.All[130]; // MH
    public static BitCountry Martinique => Table.All[131]; // MQ
    public static BitCountry Mauritania => Table.All[132]; // MR
    public static BitCountry Mauritius => Table.All[133]; // MU
    public static BitCountry Mayotte => Table.All[134]; // YT
    public static BitCountry Mexico => Table.All[135]; // MX
    public static BitCountry Micronesia => Table.All[136]; // FM
    public static BitCountry Moldova => Table.All[137]; // MD
    public static BitCountry Monaco => Table.All[138]; // MC
    public static BitCountry Mongolia => Table.All[139]; // MN
    public static BitCountry Montenegro => Table.All[140]; // ME
    public static BitCountry Montserrat => Table.All[141]; // MS
    public static BitCountry Morocco => Table.All[142]; // MA
    public static BitCountry Mozambique => Table.All[143]; // MZ
    public static BitCountry Myanmar => Table.All[144]; // MM
    public static BitCountry Namibia => Table.All[145]; // NA
    public static BitCountry Nauru => Table.All[146]; // NR
    public static BitCountry Nepal => Table.All[147]; // NP
    public static BitCountry Netherlands => Table.All[148]; // NL
    public static BitCountry NetherlandsAntilles => Table.All[149]; // AN
    public static BitCountry NewCaledonia => Table.All[150]; // NC
    public static BitCountry NewZealand => Table.All[151]; // NZ
    public static BitCountry Nicaragua => Table.All[152]; // NI
    public static BitCountry Niger => Table.All[153]; // NE
    public static BitCountry Nigeria => Table.All[154]; // NG
    public static BitCountry Niue => Table.All[155]; // NU
    public static BitCountry NorfolkIsland => Table.All[156]; // NF
    public static BitCountry NorthKorea => Table.All[157]; // KP
    public static BitCountry NorthernMarianaIslands => Table.All[158]; // MP
    public static BitCountry Norway => Table.All[159]; // NO
    public static BitCountry Oman => Table.All[160]; // OM
    public static BitCountry Pakistan => Table.All[161]; // PK
    public static BitCountry Palau => Table.All[162]; // PW
    public static BitCountry Palestine => Table.All[163]; // PS
    public static BitCountry Panama => Table.All[164]; // PA
    public static BitCountry PapuaNewGuinea => Table.All[165]; // PG
    public static BitCountry Paraguay => Table.All[166]; // PY
    public static BitCountry Peru => Table.All[167]; // PE
    public static BitCountry Philippines => Table.All[168]; // PH
    public static BitCountry Pitcairn => Table.All[169]; // PN
    public static BitCountry Poland => Table.All[170]; // PL
    public static BitCountry Portugal => Table.All[171]; // PT
    public static BitCountry PuertoRico => Table.All[172]; // PR
    public static BitCountry Qatar => Table.All[173]; // QA
    public static BitCountry RepublicOfTheCongo => Table.All[174]; // CG
    public static BitCountry Reunion => Table.All[175]; // RE
    public static BitCountry Romania => Table.All[176]; // RO
    public static BitCountry Russia => Table.All[177]; // RU
    public static BitCountry Rwanda => Table.All[178]; // RW
    //public static BitCountry SaintBarthelemy; // BL, no flag image ships for it
    public static BitCountry SaintHelena => Table.All[179]; // SH
    public static BitCountry SaintKittsAndNevis => Table.All[180]; // KN
    public static BitCountry SaintLucia => Table.All[181]; // LC
    public static BitCountry SaintMartin => Table.All[182]; // MF
    //public static BitCountry SaintPierreAndMiquelon; // PM, no flag image ships for it
    public static BitCountry SaintVincentAndTheGrenadines => Table.All[183]; // VC
    public static BitCountry Samoa => Table.All[184]; // WS
    public static BitCountry SanMarino => Table.All[185]; // SM
    public static BitCountry SaoTomeAndPrincipe => Table.All[186]; // ST
    public static BitCountry SaudiArabia => Table.All[187]; // SA
    public static BitCountry Senegal => Table.All[188]; // SN
    public static BitCountry Serbia => Table.All[189]; // RS
    public static BitCountry Seychelles => Table.All[190]; // SC
    public static BitCountry SierraLeone => Table.All[191]; // SL
    public static BitCountry Singapore => Table.All[192]; // SG
    //public static BitCountry Sint; // SX, no flag image ships for it
    public static BitCountry Slovakia => Table.All[193]; // SK
    public static BitCountry Slovenia => Table.All[194]; // SI
    public static BitCountry SolomonIslands => Table.All[195]; // SB
    public static BitCountry Somalia => Table.All[196]; // SO
    public static BitCountry SouthAfrica => Table.All[197]; // ZA
    public static BitCountry SouthGeorgiaAndTheSouthSandwichIslands => Table.All[198]; // GS
    public static BitCountry SouthKorea => Table.All[199]; // KR
    public static BitCountry SouthSudan => Table.All[200]; // SS
    public static BitCountry Spain => Table.All[201]; // ES
    public static BitCountry SriLanka => Table.All[202]; // LK
    public static BitCountry Sudan => Table.All[203]; // SD
    public static BitCountry Suriname => Table.All[204]; // SR
    //public static BitCountry SvalbardAndJanMayen; // SJ, no flag image ships for it
    public static BitCountry Swaziland => Table.All[205]; // SZ
    public static BitCountry Sweden => Table.All[206]; // SE
    public static BitCountry Switzerland => Table.All[207]; // CH
    public static BitCountry Syria => Table.All[208]; // SY
    public static BitCountry Taiwan => Table.All[209]; // TW
    public static BitCountry Tajikistan => Table.All[210]; // TJ
    public static BitCountry Tanzania => Table.All[211]; // TZ
    public static BitCountry Thailand => Table.All[212]; // TH
    public static BitCountry Togo => Table.All[213]; // TG
    public static BitCountry Tokelau => Table.All[214]; // TK
    public static BitCountry Tonga => Table.All[215]; // TO
    public static BitCountry TrinidadAndTobago => Table.All[216]; // TT
    public static BitCountry Tunisia => Table.All[217]; // TN
    public static BitCountry Turkey => Table.All[218]; // TR
    public static BitCountry Turkmenistan => Table.All[219]; // TM
    public static BitCountry TurksAndCaicosIslands => Table.All[220]; // TC
    public static BitCountry Tuvalu => Table.All[221]; // TV
    public static BitCountry USVirginIslands => Table.All[222]; // VI
    public static BitCountry Uganda => Table.All[223]; // UG
    public static BitCountry Ukraine => Table.All[224]; // UA
    public static BitCountry UnitedArabEmirates => Table.All[225]; // AE
    public static BitCountry UnitedKingdom => Table.All[226]; // GB
    public static BitCountry UnitedStates => Table.All[227]; // US
    public static BitCountry Uruguay => Table.All[228]; // UY
    public static BitCountry Uzbekistan => Table.All[229]; // UZ
    public static BitCountry Vanuatu => Table.All[230]; // VU
    public static BitCountry Vatican => Table.All[231]; // VA
    public static BitCountry Venezuela => Table.All[232]; // VE
    public static BitCountry Vietnam => Table.All[233]; // VN
    public static BitCountry WallisAndFutuna => Table.All[234]; // WF
    public static BitCountry WesternSahara => Table.All[235]; // EH
    public static BitCountry Yemen => Table.All[236]; // YE
    public static BitCountry Zambia => Table.All[237]; // ZM
    public static BitCountry Zimbabwe => Table.All[238]; // ZW

    /// <summary>
    /// Every country of the table, in alphabetical order.
    /// </summary>
    public static BitCountry[] All => Table.All;

    // The table every country is read from, one per line: the name, the dialing code, the alpha-2 and alpha-3 codes,
    // and - where it has them - the priority among the countries sharing its dialing code and the other dialing codes
    // it answers to. A line starting with // is a country no flag image ships for.
    //
    // It is one string rather than a field per country, so a page that names a single country - the DefaultCountry
    // of a BitPhoneInputParams set up as the app starts - does not run a static constructor creating every one of them:
    // a WebAssembly app's interpreter transforms such a constructor as one very large method before it runs it, which
    // costs far more of the start than the countries themselves. The string is read in one loop the first time any
    // country is asked for.
    private const string Rows = """
           Afghanistan                                  | 93      | AF | AFG
           Åland Islands                                | 358-18  | AX | ALA
           Albania                                      | 355     | AL | ALB
           Algeria                                      | 213     | DZ | DZA
           American Samoa                               | 1-684   | AS | ASM
           Andorra                                      | 376     | AD | AND
           Angola                                       | 244     | AO | AGO
           Anguilla                                     | 1-264   | AI | AIA
           Antarctica                                   | 672     | AQ | ATA
           Antigua and Barbuda                          | 1-268   | AG | ATG
           Argentina                                    | 54      | AR | ARG
           Armenia                                      | 374     | AM | ARM
           Aruba                                        | 297     | AW | ABW
           Australia                                    | 61      | AU | AUS
           Austria                                      | 43      | AT | AUT
           Azerbaijan                                   | 994     | AZ | AZE
           Bahamas                                      | 1-242   | BS | BHS
           Bahrain                                      | 973     | BH | BHR
           Bangladesh                                   | 880     | BD | BGD
           Barbados                                     | 1-246   | BB | BRB
           Belarus                                      | 375     | BY | BLR
           Belgium                                      | 32      | BE | BEL
           Belize                                       | 501     | BZ | BLZ
           Benin                                        | 229     | BJ | BEN
           Bermuda                                      | 1-441   | BM | BMU
           Bhutan                                       | 975     | BT | BTN
           Bolivia                                      | 591     | BO | BOL
           Bosnia and Herzegovina                       | 387     | BA | BIH
           Botswana                                     | 267     | BW | BWA
           Brazil                                       | 55      | BR | BRA
        // British Indian Ocean Territory               | 246     | IO | IOT
           British Virgin Islands                       | 1-284   | VG | VGB
           Brunei                                       | 673     | BN | BRN
           Bulgaria                                     | 359     | BG | BGR
           Burkina Faso                                 | 226     | BF | BFA
           Burundi                                      | 257     | BI | BDI
           Cambodia                                     | 855     | KH | KHM
           Cameroon                                     | 237     | CM | CMR
           Canada                                       | 1       | CA | CAN
           Cape Verde                                   | 238     | CV | CPV
           Cayman Islands                               | 1-345   | KY | CYM
           Central African Republic                     | 236     | CF | CAF
           Chad                                         | 235     | TD | TCD
           Chile                                        | 56      | CL | CHL
           China                                        | 86      | CN | CHN
           Christmas Island                             | 61      | CX | CXR
           Cocos Islands                                | 61      | CC | CCK
           Colombia                                     | 57      | CO | COL
           Comoros                                      | 269     | KM | COM
           Cook Islands                                 | 682     | CK | COK
           Costa Rica                                   | 506     | CR | CRI
           Croatia                                      | 385     | HR | HRV
           Cuba                                         | 53      | CU | CUB
           Curacao                                      | 599     | CW | CUW
           Cyprus                                       | 357     | CY | CYP
           Czech Republic                               | 420     | CZ | CZE
           Democratic Republic of the Congo             | 243     | CD | COD
           Denmark                                      | 45      | DK | DNK
           Djibouti                                     | 253     | DJ | DJI
           Dominica                                     | 1-767   | DM | DMA
           Dominican Republic                           | 1-809   | DO | DOM |   | 1-829,1-849
           East Timor                                   | 670     | TL | TLS
           Ecuador                                      | 593     | EC | ECU
           Egypt                                        | 20      | EG | EGY
           El Salvador                                  | 503     | SV | SLV
           Equatorial Guinea                            | 240     | GQ | GNQ
           Eritrea                                      | 291     | ER | ERI
           Estonia                                      | 372     | EE | EST
           Ethiopia                                     | 251     | ET | ETH
           Falkland Islands                             | 500     | FK | FLK
           Faroe Islands                                | 298     | FO | FRO
           Fiji                                         | 679     | FJ | FJI
           Finland                                      | 358     | FI | FIN
           France                                       | 33      | FR | FRA
           French Polynesia                             | 689     | PF | PYF
           French Southern Territories                  | 262     | TF | ATF
           Gabon                                        | 241     | GA | GAB
           Gambia                                       | 220     | GM | GMB
           Georgia                                      | 995     | GE | GEO
           Germany                                      | 49      | DE | DEU
           Ghana                                        | 233     | GH | GHA
           Gibraltar                                    | 350     | GI | GIB
           Greece                                       | 30      | GR | GRC
           Greenland                                    | 299     | GL | GRL
           Grenada                                      | 1-473   | GD | GRD
           Guam                                         | 1-671   | GU | GUM
           Guatemala                                    | 502     | GT | GTM
           Guernsey                                     | 44-1481 | GG | GGY
           Guinea                                       | 224     | GN | GIN
           Guinea Bissau                                | 245     | GW | GNB
           Guyana                                       | 592     | GY | GUY
           Haiti                                        | 509     | HT | HTI
           Honduras                                     | 504     | HN | HND
           Hong Kong                                    | 852     | HK | HKG
           Hungary                                      | 36      | HU | HUN
           Iceland                                      | 354     | IS | ISL
           India                                        | 91      | IN | IND
           Indonesia                                    | 62      | ID | IDN
           Iran                                         | 98      | IR | IRN
           Iraq                                         | 964     | IQ | IRQ
           Ireland                                      | 353     | IE | IRL
           Isle of Man                                  | 44-1624 | IM | IMN
           Israel                                       | 972     | IL | ISR
           Italy                                        | 39      | IT | ITA
           Ivory Coast                                  | 225     | CI | CIV
           Jamaica                                      | 1-876   | JM | JAM
           Japan                                        | 81      | JP | JPN
           Jersey                                       | 44-1534 | JE | JEY
           Jordan                                       | 962     | JO | JOR
           Kazakhstan                                   | 7       | KZ | KAZ
           Kenya                                        | 254     | KE | KEN
           Kiribati                                     | 686     | KI | KIR
        // Kosovo                                       | 383     | XK | XKX
           Kuwait                                       | 965     | KW | KWT
           Kyrgyzstan                                   | 996     | KG | KGZ
           Laos                                         | 856     | LA | LAO
           Latvia                                       | 371     | LV | LVA
           Lebanon                                      | 961     | LB | LBN
           Lesotho                                      | 266     | LS | LSO
           Liberia                                      | 231     | LR | LBR
           Libya                                        | 218     | LY | LBY
           Liechtenstein                                | 423     | LI | LIE
           Lithuania                                    | 370     | LT | LTU
           Luxembourg                                   | 352     | LU | LUX
           Macau                                        | 853     | MO | MAC
           Macedonia                                    | 389     | MK | MKD
           Madagascar                                   | 261     | MG | MDG
           Malawi                                       | 265     | MW | MWI
           Malaysia                                     | 60      | MY | MYS
           Maldives                                     | 960     | MV | MDV
           Mali                                         | 223     | ML | MLI
           Malta                                        | 356     | MT | MLT
           Marshall Islands                             | 692     | MH | MHL
           Martinique                                   | 596     | MQ | MTQ
           Mauritania                                   | 222     | MR | MRT
           Mauritius                                    | 230     | MU | MUS
           Mayotte                                      | 262     | YT | MYT
           Mexico                                       | 52      | MX | MEX
           Micronesia                                   | 691     | FM | FSM
           Moldova                                      | 373     | MD | MDA
           Monaco                                       | 377     | MC | MCO
           Mongolia                                     | 976     | MN | MNG
           Montenegro                                   | 382     | ME | MNE
           Montserrat                                   | 1-664   | MS | MSR
           Morocco                                      | 212     | MA | MAR
           Mozambique                                   | 258     | MZ | MOZ
           Myanmar                                      | 95      | MM | MMR
           Namibia                                      | 264     | NA | NAM
           Nauru                                        | 674     | NR | NRU
           Nepal                                        | 977     | NP | NPL
           Netherlands                                  | 31      | NL | NLD
           Netherlands Antilles                         | 599     | AN | ANT
           New Caledonia                                | 687     | NC | NCL
           New Zealand                                  | 64      | NZ | NZL
           Nicaragua                                    | 505     | NI | NIC
           Niger                                        | 227     | NE | NER
           Nigeria                                      | 234     | NG | NGA
           Niue                                         | 683     | NU | NIU
           Norfolk Island                               | 672     | NF | NFK | 1
           North Korea                                  | 850     | KP | PRK
           Northern Mariana Islands                     | 1-670   | MP | MNP
           Norway                                       | 47      | NO | NOR
           Oman                                         | 968     | OM | OMN
           Pakistan                                     | 92      | PK | PAK
           Palau                                        | 680     | PW | PLW
           Palestine                                    | 970     | PS | PSE
           Panama                                       | 507     | PA | PAN
           Papua New Guinea                             | 675     | PG | PNG
           Paraguay                                     | 595     | PY | PRY
           Peru                                         | 51      | PE | PER
           Philippines                                  | 63      | PH | PHL
           Pitcairn                                     | 64      | PN | PCN
           Poland                                       | 48      | PL | POL
           Portugal                                     | 351     | PT | PRT
           Puerto Rico                                  | 1-787   | PR | PRI |   | 1-939
           Qatar                                        | 974     | QA | QAT
           Republic of the Congo                        | 242     | CG | COG
           Reunion                                      | 262     | RE | REU | 1
           Romania                                      | 40      | RO | ROU
           Russia                                       | 7       | RU | RUS | 1
           Rwanda                                       | 250     | RW | RWA
        // Saint Barthelemy                             | 590     | BL | BLM
           Saint Helena                                 | 290     | SH | SHN
           Saint Kitts and Nevis                        | 1-869   | KN | KNA
           Saint Lucia                                  | 1-758   | LC | LCA
           Saint Martin                                 | 590     | MF | MAF
        // Saint Pierre and Miquelon                    | 508     | PM | SPM
           Saint Vincent and the Grenadines             | 1-784   | VC | VCT
           Samoa                                        | 685     | WS | WSM
           San Marino                                   | 378     | SM | SMR
           Sao Tome and Principe                        | 239     | ST | STP
           Saudi Arabia                                 | 966     | SA | SAU
           Senegal                                      | 221     | SN | SEN
           Serbia                                       | 381     | RS | SRB
           Seychelles                                   | 248     | SC | SYC
           Sierra Leone                                 | 232     | SL | SLE
           Singapore                                    | 65      | SG | SGP
        // Sint Maarten                                 | 1-721   | SX | SXM
           Slovakia                                     | 421     | SK | SVK
           Slovenia                                     | 386     | SI | SVN
           Solomon Islands                              | 677     | SB | SLB
           Somalia                                      | 252     | SO | SOM
           South Africa                                 | 27      | ZA | ZAF
           South Georgia and the South Sandwich Islands | 500     | GS | SGS
           South Korea                                  | 82      | KR | KOR
           South Sudan                                  | 211     | SS | SSD
           Spain                                        | 34      | ES | ESP
           Sri Lanka                                    | 94      | LK | LKA
           Sudan                                        | 249     | SD | SDN
           Suriname                                     | 597     | SR | SUR
        // Svalbard and Jan Mayen                       | 47      | SJ | SJM
           Swaziland                                    | 268     | SZ | SWZ
           Sweden                                       | 46      | SE | SWE
           Switzerland                                  | 41      | CH | CHE
           Syria                                        | 963     | SY | SYR
           Taiwan                                       | 886     | TW | TWN
           Tajikistan                                   | 992     | TJ | TJK
           Tanzania                                     | 255     | TZ | TZA
           Thailand                                     | 66      | TH | THA
           Togo                                         | 228     | TG | TGO
           Tokelau                                      | 690     | TK | TKL
           Tonga                                        | 676     | TO | TON
           Trinidad and Tobago                          | 1-868   | TT | TTO
           Tunisia                                      | 216     | TN | TUN
           Turkey                                       | 90      | TR | TUR
           Turkmenistan                                 | 993     | TM | TKM
           Turks and Caicos Islands                     | 1-649   | TC | TCA
           Tuvalu                                       | 688     | TV | TUV
           U.S. Virgin Islands                          | 1-340   | VI | VIR
           Uganda                                       | 256     | UG | UGA
           Ukraine                                      | 380     | UA | UKR
           United Arab Emirates                         | 971     | AE | ARE
           United Kingdom                               | 44      | GB | GBR
           United States                                | 1       | US | USA | 1
           Uruguay                                      | 598     | UY | URY
           Uzbekistan                                   | 998     | UZ | UZB
           Vanuatu                                      | 678     | VU | VUT
           Vatican                                      | 379     | VA | VAT
           Venezuela                                    | 58      | VE | VEN
           Vietnam                                      | 84      | VN | VNM
           Wallis and Futuna                            | 681     | WF | WLF
           Western Sahara                               | 212     | EH | ESH
           Yemen                                        | 967     | YE | YEM
           Zambia                                       | 260     | ZM | ZMB
           Zimbabwe                                     | 263     | ZW | ZWE
        """;

    /// <summary>
    /// The table, read the first time anything of it is asked for: a nested class is initialized on its own, so
    /// nothing of BitCountries but what is used is built.
    /// </summary>
    /// <remarks>
    /// Every country property of BitCountries reads its own row of <see cref="All"/> by a constant index - a field load
    /// and a bounds check rather than a lookup by code on every read - in the order of the rows, which is the order
    /// the properties are declared in. BitCountriesTests pins each property to its row.
    /// </remarks>
    private static class Table
    {
        public static readonly BitCountry[] All = Read();

        private static BitCountry[] Read()
        {
            var countries = new List<BitCountry>(256);

            foreach (var line in Rows.AsSpan().EnumerateLines())
            {
                var rest = line.Trim();

                if (rest.IsEmpty || rest.StartsWith("//", StringComparison.Ordinal)) continue;

                var name = Next(ref rest);
                var code = Next(ref rest);
                var iso2 = Next(ref rest);
                var iso3 = Next(ref rest);
                var priority = Next(ref rest);
                var extraCodes = Next(ref rest);

                countries.Add(new(name,
                                  code,
                                  iso2,
                                  iso3,
                                  priority.Length == 0 ? 0 : int.Parse(priority, System.Globalization.CultureInfo.InvariantCulture),
                                  extraCodes.Length == 0 ? null : extraCodes.Split(',')));
            }

            return [.. countries];
        }

        // The next column of a line, trimmed, and the line past it.
        private static string Next(ref ReadOnlySpan<char> rest)
        {
            var separator = rest.IndexOf('|');
            var column = separator < 0 ? rest : rest[..separator];

            rest = separator < 0 ? [] : rest[(separator + 1)..];

            return column.Trim().ToString();
        }
    }

    /// <summary>
    /// The lookups of the Find methods, built the first time one of them is called.
    /// </summary>
    private static class Lookup
    {
        // Every lookup below is a dictionary rather than a scan of All: a flag resolves its country on
        // every render, and a page that renders a list of them would otherwise walk the whole table once
        // per row. The keys are compared with the ordinal case-insensitive comparer - an ISO code and a
        // dialing code are ASCII, and a culture-aware comparison over them is both slower and, in the
        // Turkish locale, wrong about the letter I.
        public static readonly Dictionary<string, BitCountry> Iso2 = CreateMap(static c => c.Iso2);
        public static readonly Dictionary<string, BitCountry> Iso3 = CreateMap(static c => c.Iso3);
        public static readonly Dictionary<string, BitCountry> Name = CreateMap(static c => c.Name);

        // The dialing codes are keyed the way a lookup normalizes one rather than the way the table writes
        // it, so the hyphen of a North American code is off both sides of the comparison: "1-242",
        // "+1 (242)" and "1242" are the same code written three ways, and all three reach the Bahamas.
        //
        // A code several countries share is the one country's that owns it in practice - the one carrying the higher
        // Priority - so "+1" is the United States and "+7" Russia, which is how BitPhoneInput reads the same code.
        public static readonly Dictionary<string, BitCountry> Code = CreateMap(static c => NormalizeCode(c.Code), preferPriority: true);

        // The names a country is also written down by, which is not the same table a second time: the ISO
        // name where this one carries the everyday name ("Czechia" for the Czech Republic), the everyday
        // name where it carries the ISO one, and the abbreviations a page is as likely to be handed as a
        // code ("UK", "USA", "UAE"). Nothing here can take a value away from the country that actually
        // carries it: the aliases are asked only once every exact lookup has missed.
        public static readonly Dictionary<string, BitCountry> Alias = CreateAliasMap();

        // Every name and every alias with its accents, spaces and punctuation taken off, so "Aland
        // Islands", "Curaçao", "Guinea-Bissau" and "US Virgin Islands" reach the same countries "Åland
        // Islands", "Curacao", "Guinea Bissau" and "U.S. Virgin Islands" do. A page is handed a country
        // name in whichever spelling its own data uses, and none of those differences is a different
        // country.
        public static readonly Dictionary<string, BitCountry> Folded = CreateFoldedMap();
    }

    // The first regional indicator symbol, U+1F1E6 REGIONAL INDICATOR SYMBOL LETTER A. The pair a
    // country's alpha-2 code maps to is the emoji flag of that country.
    private const int RegionalIndicatorA = 0x1F1E6;

    // A subdivision flag is written as a tag sequence instead: U+1F3F4 WAVING BLACK FLAG, the letters
    // of the subdivision code as tag characters, and U+E007F CANCEL TAG to close it. Only the three
    // recommended for interchange by Unicode (GetSubdivisionEmoji) are built - every other sequence draws a
    // plain black flag rather than the flag it names.
    private const int BlackFlag = 0x1F3F4;
    private const int TagBase = 0xE0000;
    private const int CancelTag = 0xE007F;

    private static Dictionary<string, BitCountry> CreateMap(Func<BitCountry, string> keySelector, bool preferPriority = false)
    {
        var map = new Dictionary<string, BitCountry>(Table.All.Length, StringComparer.OrdinalIgnoreCase);

        // Dialing codes are not unique - Canada and the United States both carry "1" - so the first
        // country of the table that carries a key keeps it, which is the country the scan this
        // replaced would have stopped at - unless a later one carries a higher priority, where that is
        // what is asked for.
        foreach (var country in Table.All)
        {
            var key = keySelector(country);

            if (map.TryAdd(key, country)) continue;

            if (preferPriority && country.Priority > map[key].Priority)
            {
                map[key] = country;
            }
        }

        return map;
    }

    private static Dictionary<string, BitCountry> CreateAliasMap()
    {
        return new Dictionary<string, BitCountry>(StringComparer.OrdinalIgnoreCase)
        {
            // "UK" is not the alpha-2 code of any country - the United Kingdom's is "GB" - but it is
            // exceptionally reserved for it by ISO 3166-1 itself, and it is what a page is handed
            // often enough that answering nothing at all would be the wrong answer.
            ["UK"] = UnitedKingdom,
            ["Great Britain"] = UnitedKingdom,
            ["United Kingdom of Great Britain and Northern Ireland"] = UnitedKingdom,

            ["USA"] = UnitedStates,
            ["America"] = UnitedStates,
            ["United States of America"] = UnitedStates,
            ["US Virgin Islands"] = USVirginIslands,
            ["United States Virgin Islands"] = USVirginIslands,

            ["UAE"] = UnitedArabEmirates,
            ["Holland"] = Netherlands,
            ["Macao"] = Macau,
            ["Burma"] = Myanmar,
            ["Viet Nam"] = Vietnam,
            ["Brunei Darussalam"] = Brunei,
            ["Russian Federation"] = Russia,
            ["Syrian Arab Republic"] = Syria,
            ["Lao People's Democratic Republic"] = Laos,
            ["Palestinian Territories"] = Palestine,
            ["State of Palestine"] = Palestine,
            ["Republic of Korea"] = SouthKorea,
            ["Korea, South"] = SouthKorea,
            ["Democratic People's Republic of Korea"] = NorthKorea,
            ["Korea, North"] = NorthKorea,

            // The countries that have been renamed since this table was written, and the ones an
            // English page is as likely to name in the local spelling as in the English one.
            ["Czechia"] = CzechRepublic,
            ["Türkiye"] = Turkey,
            ["Eswatini"] = Swaziland,
            ["North Macedonia"] = Macedonia,
            ["Cabo Verde"] = CapeVerde,
            ["Côte d'Ivoire"] = IvoryCoast,
            ["Timor-Leste"] = EastTimor,
            ["Holy See"] = Vatican,
            ["Vatican City"] = Vatican,

            // The two Congos are told apart by their capitals as often as by their names, and the
            // larger one is still written down by the name it carried until 1997.
            ["DR Congo"] = DemocraticRepublicOfTheCongo,
            ["DRC"] = DemocraticRepublicOfTheCongo,
            ["Congo-Kinshasa"] = DemocraticRepublicOfTheCongo,
            ["Zaire"] = DemocraticRepublicOfTheCongo,
            ["Congo"] = RepublicOfTheCongo,
            ["Congo-Brazzaville"] = RepublicOfTheCongo,
        };
    }

    private static Dictionary<string, BitCountry> CreateFoldedMap()
    {
        var map = new Dictionary<string, BitCountry>(Table.All.Length + Lookup.Alias.Count, StringComparer.Ordinal);

        // The names go in before the aliases, so a folded alias can never take a folded name away from
        // the country carrying it.
        foreach (var country in Table.All)
        {
            map.TryAdd(Fold(country.Name), country);
        }

        foreach (var alias in Lookup.Alias)
        {
            map.TryAdd(Fold(alias.Key), alias.Value);
        }

        return map;
    }

    // A name written down by two sources differs in its accents, its punctuation and its spacing far
    // more often than in its letters, and none of those differences is a different country. Everything
    // but the letters and the digits is dropped - the decomposition above splits an accented letter
    // into a letter and a combining mark, and the mark is not a letter - which leaves one key both
    // spellings reach.
    private static string Fold(string value)
    {
        var decomposed = value.Normalize(System.Text.NormalizationForm.FormD);
        var buffer = new char[decomposed.Length];
        var length = 0;

        foreach (var character in decomposed)
        {
            if (char.IsLetterOrDigit(character) is false) continue;

            buffer[length++] = char.ToUpperInvariant(character);
        }

        return new string(buffer, 0, length);
    }

    /// <summary>
    /// Finds the country carrying the given ISO 3166-1 alpha-2 code, case insensitively.
    /// </summary>
    /// <remarks>
    /// The exceptionally reserved code "UK" is answered with the United Kingdom, whose own code is
    /// "GB": ISO 3166-1 reserves it for exactly that country, and a page hands it over often enough
    /// that answering nothing would be the wrong answer.
    /// </remarks>
    /// <param name="iso2">
    /// The two letter code to look up. Surrounding whitespace is ignored.
    /// </param>
    /// <returns>
    /// The country carrying the code, or null where no country of <see cref="All"/> does.
    /// </returns>
    public static BitCountry? FindByIso2(string? iso2)
    {
        if (string.IsNullOrWhiteSpace(iso2)) return null;

        var trimmed = iso2!.Trim();

        return Lookup.Iso2.GetValueOrDefault(trimmed)
            // Only a two letter value is read as a code here, so the alias table's longer entries -
            // "USA", "UAE" - stay out of a lookup that promises an alpha-2 code.
            ?? (trimmed.Length == 2 ? Lookup.Alias.GetValueOrDefault(trimmed) : null);
    }

    /// <summary>
    /// Finds the country carrying the given ISO 3166-1 alpha-3 code, case insensitively.
    /// </summary>
    /// <param name="iso3">
    /// The three letter code to look up. Surrounding whitespace is ignored.
    /// </param>
    /// <returns>
    /// The country carrying the code, or null where no country of <see cref="All"/> does.
    /// </returns>
    public static BitCountry? FindByIso3(string? iso3)
    {
        if (string.IsNullOrWhiteSpace(iso3)) return null;

        return Lookup.Iso3.GetValueOrDefault(iso3!.Trim());
    }

    /// <summary>
    /// Finds the country carrying the given English name, case insensitively.
    /// </summary>
    /// <remarks>
    /// The name of the country wins over everything else, and only once it has missed are the two
    /// other ways a page writes one down tried. The first is the alternative names and the
    /// abbreviations a country is as widely known by as by the name of this table - "Czechia",
    /// "Türkiye", "Holland", "UK", "USA", "UAE", "Burma" - which is what keeps a page whose own data
    /// carries the ISO name from resolving to nothing. The second is the spelling: the accents, the
    /// punctuation and the spacing are taken off both sides of the comparison, so "Aland Islands",
    /// "Curaçao", "Guinea-Bissau" and "US Virgin Islands" reach the countries this table writes as
    /// "Åland Islands", "Curacao", "Guinea Bissau" and "U.S. Virgin Islands".
    /// <br />
    /// It is still the whole name that is matched rather than part of it: "United" resolves to
    /// nothing.
    /// </remarks>
    /// <param name="name">
    /// The name to look up, which has to be the whole name rather than part of it. Surrounding
    /// whitespace is ignored.
    /// </param>
    /// <returns>
    /// The country carrying the name, or null where no country of <see cref="All"/> does.
    /// </returns>
    public static BitCountry? FindByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var trimmed = name!.Trim();

        return Lookup.Name.GetValueOrDefault(trimmed)
            ?? Lookup.Alias.GetValueOrDefault(trimmed)
            ?? Lookup.Folded.GetValueOrDefault(Fold(trimmed));
    }

    /// <summary>
    /// Finds the country carrying the given dialing code.
    /// </summary>
    /// <remarks>
    /// The code is read the way a telephone number is written rather than as an exact key: a leading
    /// plus sign, an international prefix written out as "00", spaces, brackets and hyphens are all
    /// taken off before the lookup, so "+31", "00 31" and "31" all reach the Netherlands, and the
    /// codes of the North American Numbering Plan resolve however they are punctuated - "1-242",
    /// "+1 (242)" and "1242" are all the Bahamas.
    /// <br />
    /// Dialing codes are not unique - Canada and the United States both carry "1", Kazakhstan and
    /// Russia both carry "7" - and the country answered with is the one that owns the code in practice,
    /// the one of them carrying the highest <see cref="BitCountry.Priority"/> - the United States and
    /// Russia - and otherwise the first of them in <see cref="All"/>, which is alphabetical. Where the
    /// difference matters, look the country up by its ISO code.
    /// </remarks>
    /// <param name="code">
    /// The dialing code to look up.
    /// </param>
    /// <returns>
    /// The country owning the code, or null where no country of <see cref="All"/> carries it.
    /// </returns>
    public static BitCountry? FindByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var normalized = NormalizeCode(code!);

        return normalized.Length == 0 ? null : Lookup.Code.GetValueOrDefault(normalized);
    }

    /// <summary>
    /// Finds the country a single value stands for, whichever of the four ways of writing one it is.
    /// </summary>
    /// <remarks>
    /// The value is read as an ISO 3166-1 alpha-2 code, an alpha-3 code, an English name and a dialing
    /// code, in that order, and the first of them that resolves wins. The length rules most of them
    /// out at once - only a two letter value can be an alpha-2 code - so the order only decides
    /// between a name and a dialing code, which cannot collide.
    /// <br />
    /// The name step is <see cref="FindByName"/>'s, so the alternative names, the abbreviations and
    /// the spellings it answers to are answered here as well: "UK", "USA", "Czechia" and "Curaçao"
    /// all resolve.
    /// </remarks>
    /// <param name="value">
    /// The alpha-2 code, alpha-3 code, name or dialing code to look up.
    /// </param>
    /// <returns>
    /// The country the value stands for, or null where it stands for none of <see cref="All"/>.
    /// </returns>
    public static BitCountry? Find(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value!.Trim();

        // A miss falls through to the step below rather than ending the lookup: a two letter value
        // that is no alpha-2 code can still be a dialing code, and "31" is the Netherlands.
        if (trimmed.Length == 2 && FindByIso2(trimmed) is { } byIso2) return byIso2;

        if (trimmed.Length == 3 && FindByIso3(trimmed) is { } byIso3) return byIso3;

        if (FindByName(trimmed) is { } byName) return byName;

        return FindByCode(trimmed);
    }

    /// <summary>
    /// Determines whether the given ISO 3166-1 alpha-2 code is one of the countries of
    /// <see cref="All"/>, and so one the packaged flag images cover.
    /// </summary>
    /// <remarks>
    /// It answers for whatever <see cref="FindByIso2"/> resolves, so the exceptionally reserved "UK"
    /// is answered for the flag the United Kingdom ships under "GB".
    /// </remarks>
    /// <param name="iso2">
    /// The two letter code to check, case insensitively.
    /// </param>
    /// <returns>
    /// true where a flag image ships for the code; otherwise, false.
    /// </returns>
    public static bool HasFlag(string? iso2)
    {
        return FindByIso2(iso2) is not null;
    }

    /// <summary>
    /// Builds the Unicode emoji flag of the given ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <remarks>
    /// The emoji flag of a country is the pair of regional indicator symbols its two letters stand
    /// for, so the code is turned into one rather than looked up - which is why this answers for any
    /// pair of ASCII letters, including the ones <see cref="All"/> does not carry. What such a pair
    /// looks like is the platform's to decide: an emoji font that has no flag for it draws the two
    /// letters side by side instead.
    /// <br />
    /// The three subdivisions Unicode recommends for interchange - "GB-ENG", "GB-SCT" and "GB-WLS" -
    /// are answered as well, as the tag sequence their codes stand for. Every other subdivision draws
    /// a plain black flag rather than the flag it names, so it is answered with nothing at all.
    /// </remarks>
    /// <param name="iso2">
    /// The two letter code to build the emoji flag of, or one of the three subdivision codes above.
    /// Surrounding whitespace is ignored.
    /// </param>
    /// <returns>
    /// The emoji flag, or null where the code is neither two ASCII letters nor one of those three.
    /// </returns>
    public static string? GetEmoji(string? iso2)
    {
        if (string.IsNullOrWhiteSpace(iso2)) return null;

        var code = iso2!.Trim();

        if (code.Length != 2) return GetSubdivisionEmoji(code);

        var first = char.ToUpperInvariant(code[0]);
        var second = char.ToUpperInvariant(code[1]);

        if (char.IsAsciiLetterUpper(first) is false || char.IsAsciiLetterUpper(second) is false) return null;

        return string.Concat(char.ConvertFromUtf32(RegionalIndicatorA + (first - 'A')),
                             char.ConvertFromUtf32(RegionalIndicatorA + (second - 'A')));
    }

    // A subdivision flag is a waving black flag followed by the letters of its ISO 3166-2 code written
    // as tag characters, which the emoji font replaces with the flag as a whole. The hyphen is not
    // part of the sequence, so "GB-ENG" and "gbeng" are the same flag.
    private static string? GetSubdivisionEmoji(string code)
    {
        var folded = Fold(code);

        if (folded is not ("GBENG" or "GBSCT" or "GBWLS")) return null;

        var builder = new System.Text.StringBuilder(2 + (folded.Length * 2) + 2);

        builder.Append(char.ConvertFromUtf32(BlackFlag));

        foreach (var character in folded)
        {
            builder.Append(char.ConvertFromUtf32(TagBase + char.ToLowerInvariant(character)));
        }

        return builder.Append(char.ConvertFromUtf32(CancelTag)).ToString();
    }

    // A dialing code is written in as many ways as a telephone number is, and all of them mean the
    // same country: the plus sign and the "00" that stands in for it are prefixes rather than part of
    // the code, and the spaces, brackets and hyphens are only there to be read. The codes of the North
    // American Numbering Plan are the ones this matters for - the table writes them "1-242", a page is
    // as likely to hand over "+1 (242)" - and the keys of the map are normalized the same way, so both
    // spellings meet in the middle at "1242".
    private static string NormalizeCode(string code)
    {
        var buffer = new char[code.Length];
        var length = 0;

        foreach (var character in code)
        {
            if (char.IsWhiteSpace(character) || character is '+' or '(' or ')' or '-') continue;

            buffer[length++] = character;
        }

        // Only a prefix is taken off, never the whole code: a code that is nothing but "00" is not a
        // prefix of anything.
        if (length > 2 && buffer[0] is '0' && buffer[1] is '0') return new string(buffer, 2, length - 2);

        return new string(buffer, 0, length);
    }
}
