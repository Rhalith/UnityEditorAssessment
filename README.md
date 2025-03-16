# Unity GameObject Manager (Custom Editor Tool)

## Overview
This Unity Editor tool provides an efficient way to **list, filter, sort, and edit GameObjects** within a scene. It allows developers to batch modify GameObjects, apply filters, and toggle their active states—all from a convenient custom editor window.

## Features
✅ **GameObject List:** Displays all GameObjects in the scene with their names and active states.  
✅ **Search Bar:** Quickly find GameObjects by name.  
✅ **Filtering:** Show only GameObjects with specific components:  
   - ✅ Mesh Renderer  
   - ✅ Collider  
   - ✅ Rigidbody  
✅ **Sorting:** Organize objects alphabetically, by active state, tag, or layer.  
✅ **Multi-Selection & Batch Editing:** Modify Position, Rotation, Scale, or active state for multiple objects at once.  
✅ **Component Operations:**  
   - Add a component to all selected objects  
   - Remove a component from all selected objects  
✅ **Undo/Redo Support:** All modifications are reversible.  

---

## Installation
1. Clone the repository:
   ```sh
   git clone https://github.com/Rhalith/UnityEditorAssessment.git
   ```
2. Open the project in **Unity 6000.0.40f1**.
3. Navigate to `Tools > GameObject Manager` in Unity Editor to access the tool.

---

## Usage Guide

### Opening the Tool
- In Unity Editor, go to **Tools > GameObject Manager**.

### Searching for GameObjects
- Use the **Search Bar** to filter objects by name.

### Applying Filters
- Enable filters to show only GameObjects containing:
  - Mesh Renderer
  - Collider
  - Rigidbody
- Toggle **Show Inactive** to display hidden objects.

### Sorting Options
- **Alphabetical Order (A-Z or Z-A)**
- **Active State (Active First or Inactive First)**
- **Sort by Tag or Layer**

### Editing GameObjects
- **Select multiple objects** and modify their **Transform properties (Position, Rotation, Scale)**.
- Toggle **Active/Inactive** state directly in the list.

### Adding/Removing Components
- Select a component type from the dropdown.
- Click **Add Component** or **Remove Component** to apply changes in bulk.

### Undo/Redo
- Use **Undo/Redo buttons** within the tool to revert or apply previous changes.

---

## Technical Details
- Developed using **Unity 6000.0.40f1**.
- Uses **EditorWindow**, **Undo/Redo API**, and **Custom Filtering & Sorting**.
- **ChangeHistoryManager** tracks modifications for easy undo/redo.
- **GameObjectDataService** fetches filtered and sorted GameObjects.
- **GameObjectEditController** applies modifications in batches.

---

## Folder Structure
```
/Assets
 ├── Editor/
 │   ├── GameObjectListWindow.cs
 │   ├── GameObjectEditPopup.cs
 │   ├── GameObjectEditController.cs
 │   ├── GameObjectDataService.cs
 │   ├── ChangeHistoryManager.cs
 │   ├── FilterAndSort/
 │   │   ├── GameObjectFilter.cs
 │   │   ├── GameObjectSorter.cs
 │   │   ├── FilterMode.cs
 │   │   ├── SortType.cs
```

---

## Demo & Screenshots
📺 **Watch the Tool in Action:** [YouTube Video](https://youtu.be/e8Oej8_C-Fc) or [Download Video](Assets/Tool_Usage_Video.mp4)

![image](https://github.com/user-attachments/assets/31f43f5e-2738-4a44-9817-e0e0c0fc8ea8)
![image](https://github.com/user-attachments/assets/a570250a-b341-476e-b4e1-2439d649a24c)


---

## License
This project is licensed under the **MIT License**.

---

## Contact
For inquiries, feel free to reach out:  
📧 Email: [akmannuhyigit@gmail.com](mailto:akmannuhyigit@gmail.com)  
👨‍💻 GitHub: [Rhalith](https://github.com/Rhalith)
