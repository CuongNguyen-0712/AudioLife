# Audio Guide Design Specifications

## Category Chips (MainPage)

### Visual States

- **Unselected Chip**
  - Background Color: `SurfaceContainerHigh` (#E6E9E7 - light gray)
  - Text Color: `OnSurfaceVariant` (#3F4949 - dark gray)
- **Selected Chip**
  - Background Color: `Primary` (#13696D - teal)
  - Text Color: `OnPrimary` (#FFFFFF - white)

### Styling

- Corner Radius: 15px
- Padding: 12x6 (horizontal x vertical)
- Font: RobotoCondensed-Medium
- Font Size: 12px

### Related Style Keys

- Unselected: `ChipText` style
- Selected: `ChipTextSelected` style
- Frame: `Chip` and `ChipSelected` styles (in AppStyles.xaml)

**Location:** `VinhKhanhAudioGuide.Mobile/Resources/Styles/AppStyles.xaml`

---

## MainPage Layout

### Typography

- Hero Headline: 32px Bold, text color OnSurfaceContainerLowest
- Section Headers: 20px Bold
- Item Names (Featured): 16px Bold
- Item Names (Popular): 15px Bold
- Captions: 13-14px, OnSurfaceVariant with 0.7 opacity

### Image Sizes

- Hero Image: 320px height
- Featured Items: 180px height
- Popular Items: 80x80px

### Item Collections

- Featured Cards: 280px width, 340px height
- Popular Eateries: Full width rows with 80x80px thumbnail

---

## Color Palette (from Colors.xaml)

- Primary: #13696D (teal)
- OnPrimary: #FFFFFF (white)
- OnSurfaceVariant: #3F4949 (dark gray)
- SurfaceContainerHigh: #E6E9E7 (light gray)
- SurfaceContainerLowest: #FFFFFF (white)
