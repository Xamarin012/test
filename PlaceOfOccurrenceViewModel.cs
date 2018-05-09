using DSNY.Novas.Common;
using DSNY.Novas.Models;
using DSNY.Novas.Services;
using DSNY.Novas.ViewModels.Utils;
using Plugin.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DSNY.Novas.ViewModels
{
    public class PlaceOfOccurrenceViewModel : ViewModelBase
    {
        #region Fields

        private List<FirstDescriptors> _firstDescriptors;
        private FirstDescriptors _firstDescriptorsSelectedItem;
        private List<District> _districts;
        private District _districtSelectedItem;
        private List<Sections> _sections;
        private Sections _sectionsSelectedItem;

        private string _houseNumberBox1;
        private string _houseNumberBox2;
        private List<string> _identifiers;
        private string _identifiersSelectedItem;

        private List<LookupTable> _sides;
        private LookupTable _sidesSelectedItem;
        private List<LookupTable> _corners;
        private LookupTable _cornersSelectedItem;

        private StreetCodeMaster _firstStreet;
        private string _firstStreetLabel;
        private StreetCodeMaster _crossStreet1;
        private string _crossStreet1Label;
        private StreetCodeMaster _crossStreet2;
        private string _crossStreet2Label;

        private string _locationDescription;
        private string _locationDescriptionLabel;

        private List<BoroMaster> _boros;
        private IDictionary<string, BoroMaster> _boroughs;
        private BoroMaster _boroSelectedItem;
        private string _mdrNumber;
        private string _bblNumber;

        private bool _isBoroVisible;
        private bool _isDistrictAndSectionVisible;
        private bool _isDistrictAndSectionEnabled;
        private bool _isFirstDescriptorEnabled;
        private bool _isBoroEnabled;
        private bool _isFirstStreetEnabled;
        private bool _isAddressInfoVisible;
        private bool _isSideVisible;
        private bool _isCornerVisible;
        private bool _isFirstStreetVisible;
        private bool _isCrossStreet1Visible;
        private bool _isCrossStreet2Visible;
        private bool _isLocationDescriptionVisible;
        private bool _isMDRVisible;
        private bool _isBBLVisible;
        private bool _areFieldsEnabled;
        private bool _isMDREnabled;
        private bool _isAddressOverridden;
        private List<Violator> _repeatViolators;
        //private List<PropertyDetails> _propertyDetailsList;
        private string nextNovNum;

        private bool _isLoading;
        private bool _isPresentingStreetSearchModal;
        private bool _hasCompletedInitialLoad;
        private bool _ShouldLockPlaceOfOccurrenceOnNext;

        private readonly IPlaceOfOccurrenceService _placeOfOccurrenceService;
        private readonly ILookupService _lookupService;
        private readonly IPersonCommercialIDService _personCommercialIDService;
        private readonly INovService _novService;

        #endregion //Fields

        public ICommand BoroSelectedCommand { get; set; }

        public ICommand DistrictSelectedCommand { get; set; }


        public override string Title
        {
            get
            {
                var title = "Place of Occurrence";
                if (NovMaster.NovInformation.TicketStatus == "C" || IsCancelled)
                {
                    NovMaster.NovInformation.TicketStatus = "C";
                    title = "Place of Occurrence - Cancel";
                }
                else if (NovMaster.NovInformation.TicketStatus == "V" || IsVoidAction)
                {
                    NovMaster.NovInformation.TicketStatus = "V";
                    title = "Place of Occurrence - Void";
                }
                NotifyPropertyChanged(nameof(ShowCancelMenu));
                NotifyPropertyChanged(nameof(MenuItems));
                return title;
            }
        }

        public PlaceOfOccurrenceViewModel()
        {
            // _ShouldLockPlaceOfOccurrenceOnNext = true;
            _placeOfOccurrenceService = DependencyResolver.Get<IPlaceOfOccurrenceService>();
            _lookupService = DependencyResolver.Get<ILookupService>();
            _personCommercialIDService = DependencyResolver.Get<IPersonCommercialIDService>();
            _novService = DependencyResolver.Get<INovService>();
            IsBoroVisible = true;
            //_isBoroEnabled = true;

            BoroSelectedCommand = new Command<BoroMaster>(async selectedBoro =>
            {
                // Set the default boro based on the value the user selected
                // NovMaster.UserSession.DefaultBoroCode = selectedBoro.BoroId;

                // District/Section and Street Names are all tied to the Boro value, so we need to clear them out when the Boro changes
                Districts = await _placeOfOccurrenceService.GetDistricts(selectedBoro.BoroId);
                FirstStreet = new StreetCodeMaster();
                CrossStreet1 = new StreetCodeMaster();
                CrossStreet2 = new StreetCodeMaster();

                CrossSettings.Current.AddOrUpdateValue($"PlaceOfOccurrence_BoroId_{NovMaster.NovInformation.NovNumber}", selectedBoro.BoroId);
            });

            DistrictSelectedCommand = new Command<District>(async selectedDistrict =>
                Sections = selectedDistrict == null ? null : await _placeOfOccurrenceService.GetSections(selectedDistrict.DistrictId)
            );

            _repeatViolators = new List<Violator>();
        }

        public override async Task LoadAsync()
        {
            try
            {
                nextNovNum = (await _novService.GetNextNovNumber(true)).ToString();

                if (NovMaster.NovInformation.ViolationGroupId.In(new[] { 2, 3 }))
                {
                    if (NovMaster?.NovInformation != null && FirstStreet?.StreetName != null)
                        CalculateNovNumber();
                }
                else
                    CalculateNovNumber();

                // Don't reload the data on the screen if we're coming back from the StreetNameCheck modal
                NotifyPropertyChanged(nameof(Title));
                NotifyPropertyChanged(nameof(ShowCancelMenu));
                NotifyPropertyChanged(nameof(CancelMenuItems));

                if (_hasCompletedInitialLoad)
                    return;

                _isLoading = true;

                if (NovMaster.NovInformation.LockPlaceOfOccurrenceScreen)
                {
                    FieldsEnabled = false;
                    IsMDREnabled = false;
                    IsFirstStreetEnabled = false;
                    IsDistrictAndSectionEnabled = false;
                    IsBoroEnabled = false;
                }
                else if (NovMaster.NovInformation.IsAddressOverridden)
                {
                    FieldsEnabled = false;
                    IsMDREnabled = false;
                    IsFirstStreetEnabled = false;
                    IsDistrictAndSectionEnabled = true;
                    IsBoroEnabled = false;
                }
                else if (NovMaster.NovInformation.ViolationGroupId == 4)
                {
                    IsFirstStreetEnabled = true;
                    IsBoroEnabled = true;
                    FieldsEnabled = true;
                    IsMDREnabled = true;
                    IsDistrictAndSectionEnabled = true;
                    // _ShouldLockPlaceOfOccurrenceOnNext = false;
                }
                else
                {
                    IsFirstStreetEnabled = true;
                    IsBoroEnabled = true;
                    FieldsEnabled = true;
                    IsMDREnabled = true;
                    IsDistrictAndSectionEnabled = true;
                }

                if (NovMaster.NovInformation.LockPlaceOfOccurrenceScreen && !string.IsNullOrEmpty(FirstStreet?.StreetName))
                {
                    IsFirstStreetEnabled = false;
                    IsBoroEnabled = false;
                }

                var novInfo = NovMaster.NovInformation;

                if ((!string.IsNullOrEmpty(FirstStreet?.StreetName) || !string.IsNullOrEmpty(novInfo.Resp1Address)) && novInfo.ViolationGroupId == 2)
                {
                    IsFirstStreetEnabled = false;
                    IsBoroEnabled = false;
                    IsMDREnabled = false;
                }


                if (FirstDescriptors == null && novInfo.ViolationTypeId != null)
                    FirstDescriptors = await _placeOfOccurrenceService.GetFirstDescriptors(novInfo.ViolationTypeId);

                if (Identifiers == null)
                    Identifiers = await _placeOfOccurrenceService.GetIdentifiers();

                if (Sides == null)
                    Sides = (await _lookupService.GetLookupTable("ONBETWEEN")).OrderBy(_ => _.MiscKey).ToList();

                if (Corners == null)
                    Corners = (await _lookupService.GetLookupTable("CORNEROF")).OrderBy(_ => _.MiscKey).ToList();

                if (Boros == null)
                    Boros = await _placeOfOccurrenceService.GetBoros();

                if (NovMaster.NovInformation.ViolationTypeId.In(new[] { "C", "M", "R" }))
                    FirstDescriptors = FirstDescriptors?.FindAll(x => x.Description != "At");


                // Load saved Boro or select a default value
                if (novInfo.ViolationGroupId != 5 || !string.IsNullOrEmpty(novInfo.PlaceBoroCode))

                {
                    BoroSelectedItem = string.IsNullOrEmpty(novInfo.PlaceBoroCode)
                    ? Boros.Find(boro => boro.BoroId.Equals(NovMaster.UserSession.DefaultBoroCode))
                    : Boros.Find(boro => boro.BoroId.Equals(novInfo.PlaceBoroCode));
                }

                // The Districts list is dependant on the selected Boro
                if (Districts == null)
                    Districts = await _placeOfOccurrenceService.GetDistricts(BoroSelectedItem?.BoroId);

                // The Section list requires a District to be chosen first
                if (!string.IsNullOrWhiteSpace(novInfo.PlaceDistrictId))
                {

                    //_isLoading = false;
                    DistrictSelectedItem = Districts.Find(district => district.DistrictId.Equals(novInfo.PlaceDistrictId));
                }

                if (DistrictSelectedItem != null && Sections == null)
                    Sections = await _placeOfOccurrenceService.GetSections(DistrictSelectedItem.DistrictId);

                if (Sections != null)
                {
                    SectionSelectedItem = Sections.Find(section => section.SectionId.Equals(novInfo.PlaceSectionId));
                }


                // If no stored First Descriptor, default to first item in the list
                if (novInfo.ViolationGroupId == 5 && novInfo.ViolationTypeId == "O") //Only for Vacant lot 
                {
                    FirstDescriptorsSelectedItem = FirstDescriptors?.Find(_ => _.Code.Equals("V"));
                    IsFirstDescriptorEnabled = false;
                }
                else if (!string.IsNullOrEmpty(novInfo.PlaceAddressDescriptor))
                {
                    FirstDescriptorsSelectedItem = FirstDescriptors?.Find(descriptor => descriptor.Code.Equals(novInfo.PlaceAddressDescriptor)) ?? FirstDescriptors.FirstOrDefault();
                    IsFirstDescriptorEnabled = true;
                }
                else
                {
                    FirstDescriptorsSelectedItem = FirstDescriptors.FirstOrDefault();
                    IsFirstDescriptorEnabled = true;
                }

                // HouseNumberBox1, HouseNumberBox2, and Identifier are all stored in a single database column in the format {HouseNumberBox1}[-{HouseNumberBox2}]{Identifier}
                if (!string.IsNullOrEmpty(novInfo.PlaceHouseNo))
                {
                    var houseNumbers = novInfo.PlaceHouseNo.Split('-');
                    HouseNumberBox1 = houseNumbers[0];

                    if (houseNumbers.Length > 1)
                        HouseNumberBox2 = houseNumbers[1];
                }

                // There is no separator between the last house number and the identifier, so we need to look for non-numeric characters to determine where it starts
                var lastHouseNumber = string.IsNullOrWhiteSpace(HouseNumberBox2) ? HouseNumberBox1 : HouseNumberBox2;
                if (!string.IsNullOrEmpty(lastHouseNumber) && !Regex.IsMatch("^[0-9]*$", lastHouseNumber)) // House Number contains an identifier
                {
                    var savedIdentifier = Regex.Replace(lastHouseNumber, "^[0-9]*", string.Empty);
                    if (lastHouseNumber.Contains("/")) // 1/2, 1/4, etc. may also be an identifier
                        savedIdentifier = Regex.Match(lastHouseNumber, "./.*$")?.Value;

                    // If we found an identifier, remove that portion of the string from the house number box
                    if (HouseNumberBox1 != null)
                        HouseNumberBox1 = Regex.Replace(HouseNumberBox1, savedIdentifier, string.Empty);

                    if (HouseNumberBox2 != null)
                        HouseNumberBox2 = Regex.Replace(HouseNumberBox2, savedIdentifier, string.Empty);

                    IdentifiersSelectedItem = Identifiers.Find(identifier => identifier.Trim().Equals(savedIdentifier?.Trim()));
                }

                // Look up street names
                if (novInfo.PlaceStreetId != 0)
                    FirstStreet = await _placeOfOccurrenceService.LookUpByStreetCode(novInfo.PlaceStreetId, BoroSelectedItem?.BoroId);

                if (novInfo.PlaceCross1StreetId != 0)
                    CrossStreet1 = await _placeOfOccurrenceService.LookUpByStreetCode(novInfo.PlaceCross1StreetId, BoroSelectedItem?.BoroId);

                if (novInfo.PlaceCross2StreetId != 0)
                    CrossStreet2 = await _placeOfOccurrenceService.LookUpByStreetCode(novInfo.PlaceCross2StreetId, BoroSelectedItem?.BoroId);

                if (FirstStreet == null)
                    FirstStreet = new StreetCodeMaster();

                if (CrossStreet1 == null)
                    CrossStreet1 = new StreetCodeMaster();

                if (CrossStreet2 == null)
                    CrossStreet2 = new StreetCodeMaster();

                LocationDescription = novInfo.FreeAddrees;

                //hack to prevent MDR number from showing up as zero in Place of Occurrence by default
                //We want MDR number be to empty until user enters a number then save and show that number on return 
                //Here we assume when Resp1Address(street name) is already entered, then show whatever number is saved in MDR 
                //And it's not the first time they enter the screen. 
                if (NovMaster.NovInformation.Resp1Address == null)
                {
                    if (novInfo.MDRNumber != 0)
                        MDRNumber = novInfo.MDRNumber.ToString();
                }
                else
                    MDRNumber = novInfo.MDRNumber.ToString();

                // Related to NH-960, NH-961 and NH-975    
                if (!string.IsNullOrEmpty(novInfo.PlaceBBL))
                {
                    BBLNumber = novInfo.PlaceBBL;

                    NovMaster.UserSession.DefaultBoroCode = BBLNumber.Substring(0, 1);
                    BoroSelectedItem = Boros.Find(boro => boro.BoroId.Equals(NovMaster.UserSession.DefaultBoroCode));
                    _isLoading = false;
                    if (novInfo.PlaceDistrictId == null)
                        DistrictSelectedItem = Districts[0];
                    else
                        DistrictSelectedItem = Districts.Find(district => district.DistrictId.Equals(novInfo.PlaceDistrictId));

                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        if (DistrictSelectedItem != null)
                        {
                            Sections = await _placeOfOccurrenceService.GetSections(DistrictSelectedItem.DistrictId);
                            if (Sections.Count > 0)
                            {
                                if (novInfo.PlaceSectionId == null)
                                    SectionSelectedItem = Sections[0];
                                else
                                    SectionSelectedItem = Sections.Find(sections => sections.SectionId.Equals(novInfo.PlaceSectionId));
                            }

                        }
                    });
                }

                //End NH-960

                //NH-552 - hide boro for vacant lots
                //IsBoroVisible = NovMaster.ViolationGroup.TypeName != "Vacantlot";

                _isLoading = false;
                _hasCompletedInitialLoad = true;
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
        }

        public ICommand FirstStreetLookupCommand => new Command(async () =>
        {
            _isPresentingStreetSearchModal = true;

            var vm = new StreetNameCheckViewModel { NovMaster = NovMaster, BoroId = BoroSelectedItem.BoroId };
            await NavigationService.PushModalAsync(vm);
            await vm.ExecuteSearch();
            vm.StreetNameSelectedCommand = new Command(async street =>
            {
                if (street != null)
                {
                    FirstStreet = street as StreetCodeMaster;
                    //TODO: Disable firststreet textbox and boro on PoC screen and previous screen when pressing back button 

                    if (!FirstDescriptorsSelectedItem.Code.In(new[] { "B", "C", "N", "P" }) && !NovInformation.ViolationGroupId.In(new[] { 1, 4 }))
                    {
                        IsFirstStreetEnabled = false;
                        IsBoroEnabled = false;
                    }

                    NovMaster.NovInformation.LockPreviousScreens = true;
                }
            });
        });

        public ICommand CrossStreet1LookupCommand => new Command(async () =>
        {
            _isPresentingStreetSearchModal = true;

            var vm = new StreetNameCheckViewModel { NovMaster = NovMaster, BoroId = BoroSelectedItem?.BoroId };
            await NavigationService.PushModalAsync(vm);
            await vm.ExecuteSearch();
            vm.StreetNameSelectedCommand = new Command(street =>
            {
                if (street != null)
                    CrossStreet1 = street as StreetCodeMaster;
            });
        });

        public ICommand CrossStreet2LookupCommand => new Command(async () =>
        {
            _isPresentingStreetSearchModal = true;

            var vm = new StreetNameCheckViewModel { NovMaster = NovMaster, BoroId = BoroSelectedItem.BoroId };
            await NavigationService.PushModalAsync(vm);
            await vm.ExecuteSearch();
            vm.StreetNameSelectedCommand = new Command(street =>
            {
                if (street != null)
                    CrossStreet2 = street as StreetCodeMaster;
            });
        });

        public List<FirstDescriptors> FirstDescriptors
        {
            get => _firstDescriptors;
            set { _firstDescriptors = value; NotifyPropertyChanged(); }
        }

        public FirstDescriptors FirstDescriptorsSelectedItem
        {
            get => _firstDescriptorsSelectedItem;
            set
            {
                _firstDescriptorsSelectedItem = value;
                if (_firstDescriptorsSelectedItem != null)
                {
                    var code = FirstDescriptorsSelectedItem.Code;
                    if (!string.IsNullOrEmpty(code))
                    {
                        IsDistrictAndSectionVisible = code.In(new[] { "A", "B", "C", "N", "P" });

                        if (NovMaster.NovInformation.IsAddressOverridden)
                        {
                            IsDistrictAndSectionVisible = true;
                            IsDistrictAndSectionEnabled = NovMaster.NovInformation.LockPlaceOfOccurrenceScreen;

                            //if (NovMaster.NovInformation.PlaceDistrictId != null )
                            //    DistrictSelectedItem = Districts.Find(district => district.DistrictId.Equals(NovMaster.NovInformation.PlaceDistrictId));

                            //if (NovMaster.NovInformation.PlaceSectionId != null)
                            //    SectionSelectedItem = Sections.Find(section => section.SectionId.Equals(NovMaster.NovInformation.PlaceSectionId));

                            //Related to NH-911
                            IsBoroVisible = true;
                            IsBoroEnabled = !NovMaster.NovInformation.LockPlaceOfOccurrenceScreen;
                            //End NH-911
                        }

                        IsAddressInfoVisible = !code.In(new[] { "A", "B", "C", "N", "P", "V" });

                        IsSideVisible = code.Equals("B");

                        IsCornerVisible = code.Equals("C");

                        IsFirstStreetVisible = !code.In(new[] { "A", "C", "V" });

                        IsCrossStreet1Visible = code.In(new[] { "B", "C", "P" });
                        IsCrossStreet2Visible = IsCrossStreet1Visible;

                        IsLocationDescriptionVisible = code.In(new[] { "A", "N" });

                        IsMDRVisible = code.In(new[] { "F_MDR", "R_MDR", "S_MDR", "T_MDR" });

                        IsBBLVisible = code.Equals("V");

                        // Save old labels so we can compare
                        var oldFirstStreetLabel = FirstStreetLabel;
                        var oldCrossStreet1Label = CrossStreet1Label;
                        var oldCrossStreet2Label = CrossStreet2Label;

                        // First Street Name
                        if (code.In(new[] { "B", "P" }))
                            FirstStreetLabel = "ON Street Name";
                        else if (code.Equals("N"))
                            FirstStreetLabel = "Closest Street Name";
                        else if (code.Equals("Q"))
                            FirstStreetLabel = "Street Name on the Center Median";
                        else
                            FirstStreetLabel = "First Street Name";

                        // Cross Street 1
                        if (code.Equals("C"))
                            CrossStreet1Label = "OF Street Name";
                        else if (code.Equals("B"))
                            CrossStreet1Label = "BETWEEN Street Name";
                        else if (code.Equals("P"))
                            CrossStreet1Label = "On the Center Median Between Street Name";

                        // Cross Street 2
                        CrossStreet2Label = "AND Street Name";

                        // If a street name field changed title / visibility, clear it out
                        if (!IsFirstStreetVisible || !FirstStreetLabel.Equals(oldFirstStreetLabel))
                            FirstStreet = new StreetCodeMaster();

                        if (!IsCrossStreet1Visible || !CrossStreet1Label.Equals(oldCrossStreet1Label))
                            CrossStreet1 = new StreetCodeMaster();

                        if (!IsCrossStreet2Visible || !CrossStreet2Label.Equals(oldCrossStreet2Label))
                            CrossStreet2 = new StreetCodeMaster();

                        // Location Description
                        LocationDescriptionLabel = code.Equals("A") ? "At" : "Description of Location";

                        // Side / Corner default values
                        if (IsSideVisible)
                            SidesSelectedItem = Sides.Find(side => side.Code.Equals(NovMaster.NovInformation.PlaceSideOfStreet)) ?? Sides.FirstOrDefault();

                        if (IsCornerVisible)
                            CornersSelectedItem = Corners.Find(corner => corner.Code.Equals(NovMaster.NovInformation.PlaceSideOfStreet)) ?? Corners.FirstOrDefault();
                    }
                }

                NotifyPropertyChanged();
            }
        }

        public List<District> Districts
        {
            get => _districts;
            set { _districts = value; NotifyPropertyChanged(); }
        }

        public District DistrictSelectedItem
        {
            get => _districtSelectedItem;
            set
            {
                if (!_isLoading)
                {
                    _districtSelectedItem = value;
                    DistrictSelectedCommand.Execute(value);
                    NotifyPropertyChanged();
                }
            }
        }

        public List<Sections> Sections
        {
            get => _sections;
            set { _sections = value; NotifyPropertyChanged(); }
        }

        public Sections SectionSelectedItem
        {
            get => _sectionsSelectedItem;
            set
            {
                if (!_isLoading)
                    _sectionsSelectedItem = value;
                NotifyPropertyChanged();
            }
        }

        public string HouseNumberBox1
        {
            get => _houseNumberBox1;
            set
            {
                _houseNumberBox1 = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HouseNumber));
            }
        }

        public string HouseNumberBox2
        {
            get => _houseNumberBox2;
            set
            {
                _houseNumberBox2 = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HouseNumber));
            }
        }

        public List<string> Identifiers
        {
            get => _identifiers;
            set { _identifiers = value; NotifyPropertyChanged(); }
        }

        public string IdentifiersSelectedItem
        {
            get => _identifiersSelectedItem;
            set
            {
                _identifiersSelectedItem = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(HouseNumber));
            }
        }

        public List<LookupTable> Sides
        {
            get => _sides;
            set { _sides = value; NotifyPropertyChanged(); }
        }

        public LookupTable SidesSelectedItem
        {
            get => _sidesSelectedItem;
            set { _sidesSelectedItem = value; NotifyPropertyChanged(); }
        }

        public List<LookupTable> Corners
        {
            get => _corners;
            set { _corners = value; NotifyPropertyChanged(); }
        }

        public LookupTable CornersSelectedItem
        {
            get => _cornersSelectedItem;
            set { _cornersSelectedItem = value; NotifyPropertyChanged(); }
        }

        public StreetCodeMaster FirstStreet
        {
            get => _firstStreet;
            set
            {
                _firstStreet = value;

                //if (value != null && !String.IsNullOrEmpty(value.StreetName))
                //{
                //    IsFirstStreetEnabled = false;
                //    IsBoroEnabled = false;
                //}

                NotifyPropertyChanged();
            }
        }

        public string FirstStreetLabel
        {
            get => _firstStreetLabel;
            set { _firstStreetLabel = value; NotifyPropertyChanged(); }
        }

        public StreetCodeMaster CrossStreet1
        {
            get => _crossStreet1;
            set { _crossStreet1 = value; NotifyPropertyChanged(); }
        }

        public string CrossStreet1Label
        {
            get => _crossStreet1Label;
            set { _crossStreet1Label = value; NotifyPropertyChanged(); }
        }

        public StreetCodeMaster CrossStreet2
        {
            get => _crossStreet2;
            set { _crossStreet2 = value; NotifyPropertyChanged(); }
        }

        public string CrossStreet2Label
        {
            get => _crossStreet2Label;
            set { _crossStreet2Label = value; NotifyPropertyChanged(); }
        }

        public string LocationDescription
        {
            get => _locationDescription;
            set { _locationDescription = value; NotifyPropertyChanged(); }
        }

        public string LocationDescriptionLabel
        {
            get => _locationDescriptionLabel;
            set { _locationDescriptionLabel = value; NotifyPropertyChanged(); }
        }

        public List<BoroMaster> Boros
        {
            get => _boros;
            set { _boros = value; NotifyPropertyChanged(); }
        }

        public BoroMaster BoroSelectedItem
        {
            get => _boroSelectedItem;
            set
            {
                _boroSelectedItem = value;

                if (!_isLoading)
                    BoroSelectedCommand?.Execute(value);

                NotifyPropertyChanged();
            }
        }

        public string MDRNumber
        {
            get => _mdrNumber;
            set { _mdrNumber = value; NotifyPropertyChanged(); }
        }

        public string BBLNumber
        {
            get => _bblNumber;
            set { _bblNumber = value; NotifyPropertyChanged(); }
        }

        public bool FieldsEnabled
        {
            get => _areFieldsEnabled;
            set { _areFieldsEnabled = value; NotifyPropertyChanged(); }
        }


        public bool IsMDREnabled
        {
            get => _isMDREnabled;
            set { _isMDREnabled = value; NotifyPropertyChanged(); }
        }
        public bool IsAddressOverridden
        {
            get => _isAddressOverridden;
            set { _isAddressOverridden = value; NotifyPropertyChanged(); }
        }

        public bool IsDistrictAndSectionVisible
        {
            get => _isDistrictAndSectionVisible;
            set { _isDistrictAndSectionVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsDistrictAndSectionEnabled
        {
            get => _isDistrictAndSectionEnabled;
            set { _isDistrictAndSectionEnabled = value; NotifyPropertyChanged(); }
        }

        public bool IsFirstDescriptorEnabled
        {
            get => _isFirstDescriptorEnabled;
            set
            {
                _isFirstDescriptorEnabled = value && FieldsEnabled;
                NotifyPropertyChanged();
            }
        }

        //NH-552 - hide boro for vacant lots
        public bool IsBoroVisible
        {
            get => _isBoroVisible;
            set
            {
                if (_isBoroVisible == value)
                    return;

                _isBoroVisible = value;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsBoroEnabled));
            }
        }

        public bool IsBoroEnabled
        {
            //NH-552 - hide boro for vacant lots
            get => IsBoroVisible && _isBoroEnabled;
            set
            {
                if (_isBoroEnabled == value)
                    return;

                _isBoroEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsFirstStreetEnabled
        {
            get => _isFirstStreetEnabled;
            set
            {
                _isFirstStreetEnabled = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsAddressInfoVisible
        {
            get => _isAddressInfoVisible;
            set { _isAddressInfoVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsSideVisible
        {
            get => _isSideVisible;
            set { _isSideVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsCornerVisible
        {
            get => _isCornerVisible;
            set { _isCornerVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsFirstStreetVisible
        {
            get => _isFirstStreetVisible;
            set { _isFirstStreetVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsCrossStreet1Visible
        {
            get => _isCrossStreet1Visible;
            set { _isCrossStreet1Visible = value; NotifyPropertyChanged(); }
        }

        public bool IsCrossStreet2Visible
        {
            get => _isCrossStreet2Visible;
            set { _isCrossStreet2Visible = value; NotifyPropertyChanged(); }
        }

        public bool IsLocationDescriptionVisible
        {
            get => _isLocationDescriptionVisible;
            set { _isLocationDescriptionVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsMDRVisible
        {
            get => _isMDRVisible;
            set { _isMDRVisible = value; NotifyPropertyChanged(); }
        }

        public bool IsBBLVisible
        {
            get => _isBBLVisible;
            set { _isBBLVisible = value; NotifyPropertyChanged(); }
        }

        private string HouseNumber
        {
            get
            {
                var houseNumber = string.IsNullOrWhiteSpace(HouseNumberBox2) ? HouseNumberBox1 : $"{HouseNumberBox1}-{HouseNumberBox2}";

                // The identifier (e.g. A, 1/2, etc.) is stored appended to the PlaceHouseNo
                return string.IsNullOrWhiteSpace(IdentifiersSelectedItem) || IdentifiersSelectedItem == "--"
                    ? houseNumber
                    : $"{houseNumber}{IdentifiersSelectedItem}";
            }
        }

        public override async Task<List<AlertViewModel>> ValidateScreen()
        {
            try
            {
                var alerts = new List<AlertViewModel>();

                if (IsDistrictAndSectionVisible)
                {
                    if (DistrictSelectedItem == null)
                        alerts.Add(new AlertViewModel("12017", WorkFlowMessages.DSNYMSG_NOV017, elementName: "District"));

                    if (SectionSelectedItem == null)
                        alerts.Add(new AlertViewModel("12018", WorkFlowMessages.DSNYMSG_NOV018, elementName: "Section"));
                }

                if (IsAddressInfoVisible && string.IsNullOrWhiteSpace(HouseNumberBox1) && string.IsNullOrWhiteSpace(HouseNumberBox2))
                    alerts.Add(new AlertViewModel("12019", WorkFlowMessages.DSNYMSG_NOV019, elementName: "HouseNumberBox1"));

                var code = FirstDescriptorsSelectedItem?.Code;

                if (code != null)
                {
                    if (IsFirstStreetVisible)
                    {
                        var firstStreetLookup = await _placeOfOccurrenceService.FindStreet(FirstStreet.StreetName, BoroSelectedItem?.BoroId);
                        if (firstStreetLookup != null)
                            FirstStreet = firstStreetLookup;
                        else
                        {
                            if (code.Equals("B") || code.Equals("P"))
                                alerts.Add(new AlertViewModel("12021", WorkFlowMessages.DSNYMSG_NOV021, elementName: "FirstStreet"));
                            else if (code.Equals("N"))
                                alerts.Add(new AlertViewModel("12031", WorkFlowMessages.DSNYMSG_NOV031, elementName: "FirstStreet"));
                            else if (code.Equals("Q"))
                                alerts.Add(new AlertViewModel("12021", WorkFlowMessages.DSNYMSG_NOV021, elementName: "FirstStreet")); // No dedicated message for "Street Name on the Center Median"
                            else
                                alerts.Add(new AlertViewModel("12027", WorkFlowMessages.DSNYMSG_NOV027, elementName: "FirstStreet"));
                        }
                    }

                    if (IsCrossStreet1Visible)
                    {
                        var crossStreet1Lookup = await _placeOfOccurrenceService.FindStreet(CrossStreet1.StreetName, BoroSelectedItem?.BoroId);
                        if (crossStreet1Lookup != null)
                            CrossStreet1 = crossStreet1Lookup;
                        else
                        {
                            if (code.Equals("C"))
                                alerts.Add(new AlertViewModel("12020", WorkFlowMessages.DSNYMSG_NOV020, elementName: "CrossStreet1"));
                            else if (code.Equals("B"))
                                alerts.Add(new AlertViewModel("12026", WorkFlowMessages.DSNYMSG_NOV026, elementName: "CrossStreet1"));
                            else if (code.Equals("P"))
                                alerts.Add(new AlertViewModel("12026A", WorkFlowMessages.DSNYMSG_NOV026A, elementName: "CrossStreet1"));
                        }
                    }

                    if (IsCrossStreet2Visible)
                    {
                        var crossStreet2Lookup = await _placeOfOccurrenceService.FindStreet(CrossStreet2.StreetName, BoroSelectedItem?.BoroId);

                        if (crossStreet2Lookup != null)
                            CrossStreet2 = crossStreet2Lookup;
                        else
                            alerts.Add(new AlertViewModel("12023", WorkFlowMessages.DSNYMSG_NOV023, elementName: "CrossStreet2"));
                    }

                    // Corner streets can't be the same
                    if (code.Equals("C") && CrossStreet1.StreetCode == CrossStreet2.StreetCode)
                        alerts.Add(new AlertViewModel("12022", WorkFlowMessages.DSNYMSG_NOV022, elementName: "CrossStreet1"));

                    // On/Between streets can't be the same
                    if (code.In(new[] { "B", "P" }))
                    {
                        if (FirstStreet.StreetCode == CrossStreet1.StreetCode)
                            alerts.Add(code.Equals("B")
                                ? new AlertViewModel("12025", WorkFlowMessages.DSNYMSG_NOV025, elementName: "FirstStreet")
                                : new AlertViewModel("12025A", WorkFlowMessages.DSNYMSG_NOV025A, elementName: "FirstStreet"));

                        if (FirstStreet.StreetCode == CrossStreet2.StreetCode)
                            alerts.Add(new AlertViewModel("12028", WorkFlowMessages.DSNYMSG_NOV028, elementName: "FirstStreet"));

                        if (CrossStreet1.StreetCode == CrossStreet2.StreetCode)
                            alerts.Add(code.Equals("B")
                                ? new AlertViewModel("12029", WorkFlowMessages.DSNYMSG_NOV029, elementName: "CrossStreet1")
                                : new AlertViewModel("12029A", WorkFlowMessages.DSNYMSG_NOV029A, elementName: "CrossStreet1"));
                    }
                }

                if (IsLocationDescriptionVisible && string.IsNullOrWhiteSpace(LocationDescription))
                    alerts.Add(new AlertViewModel("12032", WorkFlowMessages.DSNYMSG_NOV032, elementName: "LocationDescription"));

                var ni = NovInformation;

                // PropertyDetails lookup - check HouseNumber
                //var bblHouseNumberList = new List<PropertyDetails>();
                var propertyDetailsValidItem = new PropertyDetails();
                if (IsAddressInfoVisible)
                {
                    ni.PlaceHouseNo1 = HouseNumberBox1;
                    ni.PlaceHouseNo2 = HouseNumberBox2;
                    ni.PlaceHouseIndetifier = IdentifiersSelectedItem;

                    if (CrossStreet1?.StreetCode == 0)
                    {
                        var usedStreetCode = FirstStreet.StreetCode;
                        //Bug #450 fixes to pass three parameter for house
                        //propertyDetailsValidItem = await _placeOfOccurrenceService.GetPropertyDetails(usedStreetCode, HouseNumberBox1, BoroSelectedItem.BoroId);
                        propertyDetailsValidItem = await _placeOfOccurrenceService.GetPropertyDetails(FirstStreet.StreetCode, HouseNumberBox1, HouseNumberBox2, IdentifiersSelectedItem, BoroSelectedItem.BoroId);
                    }
                    else
                        propertyDetailsValidItem = await _placeOfOccurrenceService.GetPropertyDetails(CrossStreet1.StreetCode, HouseNumberBox1, BoroSelectedItem.BoroId);

                    if (!ni.ViolationTypeId.In(new[] { "A", "O" }))
                    {
                        ni.Resp1Zip = propertyDetailsValidItem?.ZipCode.ToString();
                        ni.BusinessName = propertyDetailsValidItem?.FirstName;
                        ni.Resp1StreetId = Convert.ToInt32(propertyDetailsValidItem?.StreetCode);
                    }

                    if (ni.ViolationGroupId.Equals(2) && propertyDetailsValidItem == null)
                    {
                        if (FieldsEnabled)
                            alerts.Add(new AlertViewModel("12065", WorkFlowMessages.DSNYMSG_NOV065, "Yes", okAction: VoidAction, cancelTitle: "No", shouldContinueOnOk: true, elementName: "HouseNumberBox1"));
                        else if (!IsAddressOverridden && !ni.IsAddressOverridden && ni.ViolationGroupId != 2) //The alert will need to be 12065 when we're alerting about a property not found for Residential and Multiple. For Action we can keep this alert. 
                            alerts.Add(new AlertViewModel("12040", WorkFlowMessages.DSNYMSG_NOV040, "Yes", SetAddressOverride, "No", elementName: "HouseNumberBox1"));
                    }
                    else if (propertyDetailsValidItem != null && !IsAddressOverridden && !ni.IsAddressOverridden)
                    {
                        DistrictSelectedItem = Districts.Find(_ => _.DistrictId.Equals(propertyDetailsValidItem?.DistrictId?.ToString()));

                        if (DistrictSelectedItem != null && Sections == null)
                            Sections = await _placeOfOccurrenceService.GetSections(DistrictSelectedItem.DistrictId);

                        SectionSelectedItem = Sections?.Find(_ => _.SectionId.Trim().Equals(propertyDetailsValidItem.SectionId.ToString()));

                        if (!ni.ViolationTypeId.In(new[] { "A", "O" }))
                        {
                            if (propertyDetailsValidItem.Pop == "Y" && !IsCancelled && ni.TicketStatus != "C") //and not cancelled
                            {
                                if (ni.ViolationTypeId != "C")
                                    alerts.Add(new AlertViewModel("12038", WorkFlowMessages.DSNYMSG_NOV038, okTitle: "Yes", okAction: Cancel, cancelTitle: "No", shouldContinueOnOk: true, elementName: "HouseNumberBox1"));
                                else
                                {
                                    alerts.Add(new AlertViewModel("12038", WorkFlowMessages.DSNYMSG_NOV038, okTitle: "Yes", cancelTitle: "No", shouldContinueOnOk: true, elementName: "HouseNumberBox1"));
                                    alerts.Add(new AlertViewModel("12067", WorkFlowMessages.DSNYMSG_NOV067, okTitle: "Yes", cancelAction: Cancel, cancelTitle: "No", shouldContinueOnOk: true));
                                }
                            }
                        }

                        if (IsMDRVisible && FieldsEnabled && !IsVoidAction && ni.TicketStatus != "V") //and NOV not void
                        {
                            if (string.IsNullOrWhiteSpace(MDRNumber))
                                alerts.Add(new AlertViewModel("12037", WorkFlowMessages.DSNYMSG_NOV037, elementName: "MDRNumber"));
                            else
                            {
                                if (Regex.IsMatch(MDRNumber, "^[0]+$"))
                                    MDRNumber = "0";

                                var mdrFound = MDRNumber == "0" || propertyDetailsValidItem.MdrNo.Equals(Convert.ToInt32(MDRNumber));

                                if (!mdrFound && !IsVoidAction && ni.TicketStatus != "V") // and not Void
                                {
                                    // fix for bug # NH-467    fasfmasldf
                                    //DSNYMSG_NOV039
                                    //alerts.Add(new AlertViewModel("12039", WorkFlowMessages.DSNYMSG_NOV065, okTitle: "Yes", cancelTitle: "No", shouldContinueOnOk: true));
                                    alerts.Add(new AlertViewModel("12039", WorkFlowMessages.DSNYMSG_NOV039, okTitle: "Yes", cancelTitle: "No", shouldContinueOnOk: true, elementName: "MDRNumber"));
                                }
                                else
                                {
                                    var idType = (await _personCommercialIDService.GetIDTypes()).FirstOrDefault(_ => _.IdType.Equals("BRN"));

                                    ni.LicenseType = "BRN";
                                    ni.LicenseTypeDesc = idType?.IdDesc;
                                    ni.LicenseAgency = idType?.IssuedBy;
                                    ni.LicenseNumber = MDRNumber;
                                }
                            }
                        }
                    }
                }
                else if (IsBBLVisible)
                {
                    if (string.IsNullOrWhiteSpace(BBLNumber))
                        alerts.Add(new AlertViewModel("12033", WorkFlowMessages.DSNYMSG_NOV033, elementName: "BBLNumber"));
                    else if (BBLNumber.Length != 10)
                        alerts.Add(new AlertViewModel("12034", WorkFlowMessages.DSNYMSG_NOV034, elementName: "BBLNumber"));
                    else if (!Regex.IsMatch(BBLNumber.Substring(0, 1), "[1-5]"))
                        alerts.Add(new AlertViewModel("12035", WorkFlowMessages.DSNYMSG_NOV035, elementName: "BBLNumber"));
                    else if (ni.ViolationGroupId.In(new[] { 4, 6 }) && ni.Resp1BoroCode[0] != BBLNumber[0])
                        alerts.Add(new AlertViewModel("12036", WorkFlowMessages.DSNYMSG_NOV036, elementName: "BBLNumber"));
                    else if (ni.ViolationGroupId.In(new[] { 1, 2, 3 }) && BoroSelectedItem.BoroId[0] != BBLNumber[0])
                        alerts.Add(new AlertViewModel("12036", WorkFlowMessages.DSNYMSG_NOV036, elementName: "BBLNumber"));
                    else
                    {
                        // Fix to NH-960
                        //NovMaster.UserSession.DefaultBoroCode = BBLNumber.Substring(0, 1);
                        //BoroSelectedItem = Boros.Find(boro => boro.BoroId.Equals(NovMaster.UserSession.DefaultBoroCode));

                        var bblList = await _placeOfOccurrenceService.GetAllBBLNumbers(BBLNumber);
                        if (bblList.Count == 0)
                        {
                            if (!IsAddressOverridden && !ni.IsAddressOverridden && !ni.ViolationTypeId.In(new[] { "M", "R" }))
                                alerts.Add(new AlertViewModel("12042", WorkFlowMessages.DSNYMSG_NOV042, "Yes", SetAddressOverride, "No", elementName: "BBLNumber"));
                            else if (ni.ViolationTypeId.In(new[] { "M", "R" }))
                                alerts.Add(new AlertViewModel("12066", WorkFlowMessages.DSNYMSG_NOV066, "Yes", okAction: VoidAction, cancelTitle: "No", shouldContinueOnOk: true, elementName: "BBLNumber"));
                        }
                        else
                        {
                            var bblItem = bblList.Find(_ => _.VacantLot.Equals("V"));
                            if (bblItem?.VacantLot != "V")
                                alerts.Add(new AlertViewModel("12041", WorkFlowMessages.DSNYMSG_NOV041, "Yes", cancelTitle: "No", shouldContinueOnOk: true, elementName: "BBLNumber"));
                            else
                            {

                                if (DistrictSelectedItem.DistrictId != bblItem.DistrictId.ToString())
                                {
                                    DistrictSelectedItem = Districts.Find(_ => _.DistrictId.Equals(bblItem.DistrictId.ToString()));
                                    Sections = await _placeOfOccurrenceService.GetSections(Convert.ToString(bblItem.DistrictId));
                                }

                                if (SectionSelectedItem.SectionId.Trim() != bblItem.SectionId.ToString())
                                    SectionSelectedItem = Sections?.Find(_ => _.SectionId.Trim().Equals(bblItem.SectionId.ToString()));

                                if (bblItem.Pop == "Y" && ni.TicketStatus != "C" && !IsCancelled)
                                    alerts.Add(new AlertViewModel("12038", WorkFlowMessages.DSNYMSG_NOV038, okTitle: "Yes", okAction: Cancel, cancelTitle: "No", shouldContinueOnOk: true, elementName: "HouseNumberBox1")); //need to cancel this ticket if user clicks yes
                                else
                                {
                                    if (ni.ViolationGroupId != 5)
                                        ni.BusinessName = bblItem.FirstName;

                                    if (!ni.ViolationTypeId.In(new[] { "A", "O" }))
                                        ni.Resp1Zip = bblItem.ZipCode.ToString();
                                }
                            }
                        }
                    }
                }

                var shouldAskForOverride = false;
                RoutingTime routingTime = null;
                if (NovMaster.ViolationDetails?.RoutingFlag == "Y" && !IsVoidAction && ni.TicketStatus != "V")
                {
                    var type = ni.ViolationTypeId;
                    if (ni.ViolationTypeId.In(new[] { "C", "O" }))
                    {
                        //TODO: Why is this done here? 
                        if (type == "O")
                            type = "C";

                        if (!string.IsNullOrEmpty(DistrictSelectedItem?.DistrictId) && !string.IsNullOrEmpty(SectionSelectedItem?.SectionId))
                            routingTime = await _placeOfOccurrenceService.GetRoutingTimes(DistrictSelectedItem.DistrictId.Trim(), SectionSelectedItem?.SectionId.Trim(), BoroSelectedItem?.BoroId, type);
                        else if (propertyDetailsValidItem != default(PropertyDetails))
                        {
                            var districtID = propertyDetailsValidItem.DistrictId.ToString();
                            var sectionID = propertyDetailsValidItem.SectionId.ToString();
                            routingTime = await _placeOfOccurrenceService.GetRoutingTimes(districtID.Trim(), sectionID.Trim(), BoroSelectedItem?.BoroId, type);
                        }
                    }
                    else if (ni.ViolationTypeId.In(new[] { "M", "R" }))
                    {
                        type = "R"; //Set all R and M tickets to Residential Routing Times 
                        routingTime = await _placeOfOccurrenceService.GetRoutingTimes(type);
                    }

                    if (routingTime != default(RoutingTime))
                    {
                        DateTime effectDate = routingTime.EffectDate;

                        var routingTimeAmEarly = Convert.ToDateTime(routingTime.RoutingTimeAM.Split('-')[0]).TimeOfDay;
                        var routingTimeAmLate = Convert.ToDateTime(routingTime.RoutingTimeAM.Split('-')[1]).TimeOfDay;
                        var routingTimePmEarly = Convert.ToDateTime(routingTime.RoutingTimePM.Split('-')[0]).TimeOfDay;
                        var routingTimePmLate = Convert.ToDateTime(routingTime.RoutingTimePM.Split('-')[1]).TimeOfDay;

                        if (ni.IssuedTimestamp >= effectDate)
                        {
                            shouldAskForOverride = true;

                            if (ni.IssuedTimestamp.TimeOfDay.Between(routingTimeAmEarly, routingTimeAmLate))
                            {
                                shouldAskForOverride = false;
                                //ni.RoutingTime = String.Format(String.Format("{h:mm tt}", Convert.ToDateTime(routingTimeAmEarly.ToString())) + "-" + String.Format("{h:mm tt}", Convert.ToDateTime(routingTimeAmLate.ToString())));
                                ni.RoutingTime = routingTime.RoutingTimeAM;
                            }

                            else if (ni.IssuedTimestamp.TimeOfDay.Between(routingTimePmEarly, routingTimePmLate))
                            {
                                shouldAskForOverride = false;
                                ni.RoutingTime = routingTime.RoutingTimePM;
                                //ni.RoutingTime = String.Format(Convert.ToDateTime(routingTimePmEarly.ToString()).ToString("{h:mm tt}") + "-" + Convert.ToDateTime(routingTimePmLate.ToString()).ToString("{h:mm tt}"));//, Convert.ToDateTime(routingTimePmLate.ToString())));
                            }
                        }
                    }
                }

                if (shouldAskForOverride)
                {
                    var currentTime = Convert.ToDateTime(ni.IssuedTimestamp.ToString("yyyy-MM-dd h:00tt"));
                    ni.RoutingTime = $"{currentTime:h:mmtt}-{currentTime:h:59tt}";

                    if (IsAddressOverridden && IsDistrictAndSectionEnabled)
                        alerts.Add(new AlertViewModel("12043", string.Format(WorkFlowMessages.DSNYMSG_NOV043, ni.RoutingTime), okTitle: "Yes", cancelTitle: "No", shouldContinueOnOk: true, elementName: "District"));
                    else if (FieldsEnabled)
                        alerts.Add(new AlertViewModel("12044", string.Format(WorkFlowMessages.DSNYMSG_NOV044, ni.RoutingTime), okTitle: "Yes", cancelTitle: "No", shouldContinueOnOk: true, elementName: "HouseNumberBox1"));
                }

                // Check for Repeat Violators
                if (FirstStreet != null && ni.ViolationGroupId != 5)
                    _repeatViolators = await _placeOfOccurrenceService.GetViolators(BoroSelectedItem?.BoroId, FirstStreet.StreetCode, HouseNumber, ni?.ViolationCode, propertyDetailsValidItem?.FirstName);
                else if (BBLNumber != null)
                    _repeatViolators = await _placeOfOccurrenceService.GetViolatorsForVacantLot(ni?.ViolationCode, BBLNumber);

                return alerts;
            }
            catch (Exception ex)
            {
                var stack = ex.StackTrace.ToString();
            }

            return null;
        }

        public override ViewModelBase NextViewModel
        {
            get
            {
                _hasCompletedInitialLoad = false;
                return _repeatViolators?.Count > 0 ? new ViolatorHitViewModel(new ObservableCollection<Violator>(_repeatViolators)) { NovMaster = NovMaster } : base.NextViewModel;
            }
        }
        public async void CalculateNovNumber()
        {
            if (NovMaster.NovInformation.TicketStatus == "C")
            {
                //await _novService.DeleteNovInfo();
                NovMaster.NovInformation.NovNumber = (await _novService.GetNextNovNumber(false)).GetValueOrDefault();

                CalculateCheckSum();
            }
            else
            {
                //await _novService.DeleteNovInfo();
                await _novService.UpdateNovData(NovMaster);

                CalculateCheckSum();

                if (NovMaster.NovInformation.TicketStatus == "V")
                {
                    NovMaster.UserSession.DutyHeader.VoidCount += 1;
                }
                else
                {
                    NovMaster.UserSession.DutyHeader.TicketCount += 1;
                }

                await _novService.SaveDutyHeader(NovMaster.UserSession);
            }
        }

        public void CalculateCheckSum()
        {
            var checkDigit = 0;
            int novLength = NovMaster.NovInformation.NovNumber.ToString().Length + 1;
            for (var i = 1; i <= NovMaster.NovInformation.NovNumber.ToString().Length; i++)
            {
                checkDigit += Convert.ToInt32(NovMaster.NovInformation.NovNumber.ToString().Substring(i - 1, 1)) * Convert.ToInt32(Math.Pow(2, novLength - i));
            }

            var checkSumValues = "HZJKLMNYPXR";
            NovMaster.NovInformation.CheckSum = checkSumValues[checkDigit % 11].ToString();
        }

        public override void WriteFieldValuesToNovMaster()
        {
            var ni = NovInformation;

            ni.PlaceAddressDescriptor = FirstDescriptorsSelectedItem?.Code;
            ni.PlaceDistrictId = DistrictSelectedItem?.DistrictId ?? "";
            ni.PlaceSectionId = SectionSelectedItem?.SectionId ?? "";
            ni.PlaceHouseNo1 = HouseNumberBox1;
            ni.PlaceHouseNo2 = HouseNumberBox2;
            ni.PlaceHouseIndetifier = IdentifiersSelectedItem;

            var houseNumber = HouseNumberBox1;
            if (!string.IsNullOrWhiteSpace(HouseNumberBox2))
                houseNumber = $"{HouseNumberBox1}-{HouseNumberBox2}";

            // The identifier (e.g. A, 1/2, etc.) is stored appended to the PlaceHouseNo
            if (IdentifiersSelectedItem != null && IdentifiersSelectedItem != "--")
                houseNumber += IdentifiersSelectedItem;

            ni.PlaceHouseNo = houseNumber;

            if (!ni.ViolationTypeId.In(new[] { "A", "O" }))
            {
                ni.Resp1HouseNo = houseNumber;
                ni.Resp1Address = FirstStreet?.StreetName;
                ni.Resp1City = BoroSelectedItem?.Name;
                ni.Resp1State = "NY";
            }

            ni.PlaceStreetId = FirstStreet?.StreetCode ?? 0;
            ni.PlaceCross1StreetId = CrossStreet1?.StreetCode ?? 0;
            ni.PlaceCross2StreetId = CrossStreet2?.StreetCode ?? 0;

            string sideOrCorner = null;
            if (FirstDescriptorsSelectedItem != null)
            {
                if (FirstDescriptorsSelectedItem.Code.Equals("C"))
                    sideOrCorner = CornersSelectedItem?.Code;
                else if (FirstDescriptorsSelectedItem.Code.Equals("B"))
                    sideOrCorner = SidesSelectedItem?.Code;
            }

            ni.PlaceSideOfStreet = sideOrCorner;
            ni.FreeAddrees = LocationDescription;
            ni.MDRNumber = MDRNumber == null ? 0 : Convert.ToInt32(MDRNumber);
            ni.PlaceBBL = BBLNumber;
            ni.PlaceBoroCode = BoroSelectedItem?.BoroId;

            ni.PrintViolationCode = ni.ViolationCode; //keep violation code until ViolatorHit screen

            //if they are not a repeat violator, then we know that the code printed on the ticket will be the same we originally stored
            if (_repeatViolators?.Count == 0)
                ni.IsPlaceAddressHit = ni.IsMultipleOffences = "N";
            else
                ni.IsPlaceAddressHit = ni.IsMultipleOffences = "Y";

            if (IsCancelled)
                ni.TicketStatus = "C";
            else if (IsVoidAction)
                ni.TicketStatus = "V";
        }

        public async void SetAddressOverride()
        {
            FieldsEnabled = false;
            IsMDREnabled = false;
            IsFirstDescriptorEnabled = false;
            IsFirstStreetEnabled = false;

            // Related to NH-911
            IsBoroVisible = true;
            IsBoroEnabled = true;
            // End NH-911

            IsDistrictAndSectionVisible = true;

            // Related to NH-960
            if (Districts.Any())
            {
                DistrictSelectedItem = Districts[0];
                Sections = await _placeOfOccurrenceService.GetSections(DistrictSelectedItem.DistrictId);
                if (Sections.Count > 0)
                    SectionSelectedItem = Sections[0];
            }
            //End NH-960

            IsAddressOverridden = true;
            NovMaster.NovInformation.IsAddressOverridden = IsAddressOverridden;
        }

        private bool getFormIsViolationDetailsVisited(bool defaultChecked)
        {
            var isViolationDetailsVisitedLocal = CrossSettings.Current.GetValueOrDefault("ViolationDetails_IsVisited_" + NovMaster.NovInformation.NovNumber.ToString(), defaultChecked.ToString());
            return bool.Parse(isViolationDetailsVisitedLocal);
        }
        public override List<string> CancelMenuItems
        {
            get
            {
                if (NovMaster.NovInformation.ViolationGroupId == 5 && NovMaster.NovInformation.ViolGroupName == "S") // VacantLot GroupId = 5 ; GroupName=S ; Code in { "S21", "S6V", "S7V", "S8V", "SC7" }
                //if (NovMaster.NovInformation.ViolationCode == "S6V" || NovMaster.NovInformation.ViolationCode == "S7V")
                {
                    if (NovMaster.NovInformation.TicketStatus == "V" || NovMaster.NovInformation.TicketStatus == "C")
                        return new List<string> { };

                    if (getFormIsViolationDetailsVisited(false))
                        return new List<string> { "Void" };
                    else
                        return new List<string> { "Cancel", "Void" };
                }
                if (NovMaster.NovInformation.ViolationGroupId.In(new[] { 2, 3 }) && NovMaster?.NovInformation != null && FirstStreet?.StreetName == null)
                {
                    CrossSettings.Current.AddOrUpdateValue($"ShowVoid_{nextNovNum}", "false");
                    return new List<string> { "Cancel" };
                }

                CrossSettings.Current.AddOrUpdateValue($"ShowVoid_{nextNovNum}", "true");
                return new List<string> { "Void" };
            }
        }

        public override void VoidAction()
        {
            IsVoidAction = true;
            //To Fix: NH-947
            /*
            FieldsEnabled = false;
            IsFirstDescriptorEnabled = false;
            IsFirstStreetEnabled = false;
            IsBoroEnabled = false;
            */
        }

        //if(NovMaster.NovInformation.ViolationGroupId == 4)
        public override bool ShouldLockPlaceOfOccurrenceOnNext => true;
        //public override bool ShouldLockPlaceOfOccurrenceOnNext => _ShouldLockPlaceOfOccurrenceOnNext;

        //_ShouldLockPlaceOfOccurrenceOnNext

        public override ICommand BackCommand => new Command(async () =>
        {
            if (NavigationInProgress)
                return;

            NavigationInProgress = true;

            if (UserSession != null)
                UserSession.TimeoutTimeStamp = DateTime.Now;
            else if (NovMaster?.UserSession != null)
                NovMaster.UserSession.TimeoutTimeStamp = DateTime.Now;

            WriteFieldValuesToNovMaster();
            await NavigationService.PopAsync();
            NavigationInProgress = false;

            //if (!IsFirstStreetEnabled)
            //    NovMaster.NovInformation.LockPlaceOfOccurrenceScreen = true;
        });

        public override ICommand NextCommand => new Command(async () =>
        {
            if (NavigationInProgress) { return; }
            NavigationInProgress = true;

            //var ss = DistrictSelectedItem;

            var alerts = await ValidateScreen();
            foreach (AlertViewModel alert in alerts)
            {
                if (!await AlertService.DisplayAlert(alert) || !alert.ShouldContinueOnOk)
                {
                    NavigationInProgress = false;
                    return;
                }
            }

            if (UserSession != null)
            {
                UserSession.TimeoutTimeStamp = DateTime.Now;
            }
            else if (NovMaster?.UserSession != null)
            {
                NovMaster.UserSession.TimeoutTimeStamp = DateTime.Now;
            }

            //if (DistrictSelectedItem == null)
            //{
            //    DistrictSelectedItem = ss;
            //}

            NovMaster.NovInformation.PlaceDistrictId = DistrictSelectedItem?.DistrictId ?? "";


            //CrossSettings.Current.AddOrUpdateValue("PlaceSectionId_" + NovMaster.NovInformation.NovNumber.ToString(), SectionSelectedItem.SectionId.ToString());
            //CrossSettings.Current.AddOrUpdateValue("PlaceDistrictId_" + NovMaster.NovInformation.NovNumber.ToString(), DistrictSelectedItem.DistrictId.ToString());
            WriteFieldValuesToNovMaster();

            //WriteValueToCrossSettings();
            if (ShouldSaveTicketOnNext)
            {
                await SaveTicket();
            }

            if (ShouldLockPreviousScreensOnNext)
            {
                NovMaster.NovInformation.LockPreviousScreens = true;
            }

            if (ShouldLockPlaceOfOccurrenceOnNext)
            {
                NovMaster.NovInformation.LockPlaceOfOccurrenceScreen = true;
            }

            var nextVM = NextViewModel;
            if (nextVM != null)
            {
                await NavigationService.PushAsync(nextVM);
            }

            NavigationInProgress = false;


        });
    }
}
