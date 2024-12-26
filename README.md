
# README.md

## Overview

This project provides a robust implementation to process and update IDML (InDesign Markup Language) files dynamically. It includes functionality to manipulate image content and story mappings within `.idml` files, enabling seamless customization and automation of IDML templates. The implementation primarily revolves around modifying the XML content within `.idml` archives while maintaining compatibility with the InDesign file format.

### Core Features
- **Dynamic Template Processing**: Automates updates to `.idml` templates with new content and images.
- **XML Manipulation**: Reads, modifies, and writes XML files stored within the `.idml` zip archive.
- **Image Embedding**: Converts images to Base64 and embeds them into the template.
- **Content Replacement**: Replaces text in XML files based on old-to-new mapping configurations.

---

## Code Breakdown

### 1. **EntryPoint**
The `Main` method serves as the entry point for the application. It initializes required paths and invokes the template processing logic.
- **Primary Responsibilities**:
  - Define base file paths and input configurations.
  - Pass control to `TemplateHandler`.

#### Example:
```csharp
string basePath = "<BASE_PATH>";
string imagePath = "<IMAGE_PATH>";
templateHandler.ProcessTemplate(basePath, imagePath);
```

---

### 2. **TemplateHandler**
Manages the high-level process of template updating, delegating tasks to `IDMLProcessor`.
- **Methods**:
  - `ProcessTemplate(basePath, imagePath)`: Coordinates the update process by:
    - Setting paths for original and updated `.idml` files.
    - Preparing image and text mapping configurations.
    - Delegating updates to `IDMLProcessor`.

#### Example:
```csharp
idmlProcessor.UpdateIdmlFile(
    originalIdmlFilePath,
    updatedIdmlFilePath,
    imageFilePaths,
    oldStoryMappings,
    newStoryMappings
);
```

---

### 3. **IDMLProcessor**
Handles the core logic for manipulating `.idml` files. It extracts XML entries, applies updates, and writes modified content back to the archive.

#### Key Methods:
1. **`UpdateIdmlFile`**
   - **Input**: Paths to the original and updated `.idml` files, image paths, and text mappings.
   - **Process**:
     - Opens the `.idml` archive.
     - Updates `Spread` XML for images.
     - Updates `Story` XML for content mappings.

2. **`UpdateSpreadXml`**
   - **Purpose**: Embeds images as Base64 strings into XML files in the `Spreads/` directory.
   - **Logic**:
     - Iterates over XML files.
     - Replaces `Contents` elements with Base64-encoded image strings.

3. **`UpdateStoryXml`**
   - **Purpose**: Replaces text in `Stories/` XML based on old-to-new mappings.
   - **Logic**:
     - Identifies `CharacterStyleRange` elements.
     - Replaces content values matching old mappings.

4. **`ConvertImageToBase64`**
   - Converts an image file to a Base64 string for embedding into the XML.

5. **`ExtractStoryPaths`**
   - Retrieves paths to all `Stories/` XML files in the `.idml` archive.

---

## Usage

### Prerequisites
- Install .NET (version compatible with C# 8.0+).
- Provide valid paths to:
  - **Base IDML File**: Input `.idml` template.
  - **Images**: Files to embed into the template.
  - **Old/New Mappings**: Text to replace in `Stories` XML.

### Steps to Run
1. Replace placeholders in the code (`<BASE_PATH>`, `<IMAGE_PATH>`, etc.) with actual values.
2. Compile the code using `dotnet build`.
3. Run the executable:
   ```bash
   dotnet run
   ```
4. Verify the updated `.idml` file in the specified output path.

---

## File Structure

- **Namespace: PlaceholderNamespace**
  - **Classes**:
    - `EntryPoint`: Program entry point.
    - `TemplateHandler`: Coordinates the template processing.
    - `IDMLProcessor`: Implements XML and `.idml` manipulation logic.

---

## Example Configuration

```csharp
string[] oldStoryMappings = { "Placeholder1", "Placeholder2" };
string[] newStoryMappings = { "Replacement1", "Replacement2" };

string originalIdmlFilePath = @"C:\Templates\template.idml";
string updatedIdmlFilePath = @"C:\Output\updated_template.idml";

string[] imageFilePaths = { @"C:\Images\image1.png", @"C:\Images\image2.jpg" };
```

---

## Notes

1. **Error Handling**:
   - Ensure file paths exist and have read/write permissions.
   - Validate the `.idml` file structure before processing.

2. **Extensibility**:
   - The code can be extended to support more XML manipulation or different file types.

3. **Testing**:
   - Test with small `.idml` templates for validation before processing large files. 

---

## License

This project is licensed under the [MIT License](LICENSE).
