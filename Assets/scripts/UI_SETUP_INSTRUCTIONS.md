# Chest Interaction System - UI Setup Instructions

Follow these steps to set up the UI components for the Chest Interaction System:

## 1. Create ChestItemSlot Prefab

1. Create a new UI Panel in your hierarchy
   - Right-click in Hierarchy > UI > Panel
   - Name it "ChestItemSlot"
   - Set size to 80x80 pixels

2. Add components to the ChestItemSlot:
   - Add an Image component for the slot background
     - Set color to a semi-transparent gray (RGBA: 60, 60, 60, 200)
   - Add a child Image for the item icon
     - Name it "ItemIcon"
     - Remove its source image
     - Set native size
     - Set raycast target to true
   - Add a child GameObject with TextMeshPro component
     - Name it "QuantityText"
     - Position in bottom-right corner
     - Set font size to 14
     - Set text alignment to bottom-right
   - Add a child GameObject for highlight effect
     - Name it "Highlight"
     - Add Image component with a bright border or glow effect
     - Set Active to false by default

3. Add the ChestInventorySlot script to the ChestItemSlot GameObject
   - Assign the ItemIcon, slotBackground, quantityText, and highlightEffect references

4. Create a prefab from this GameObject
   - Drag the configured ChestItemSlot from the hierarchy to your Prefabs folder

## 2. Create Chest Inventory UI Panel

1. Create a new UI Panel
   - Name it "ChestInventoryPanel"
   - Set anchors to center
   - Size approximately 500x400 pixels
   - Add a background image or color

2. Add a header text (TextMeshPro)
   - "Chest Contents"
   - Position at top of panel

3. Create a Grid Layout Group for the slots
   - Add a child GameObject named "SlotsContainer"
   - Add Grid Layout Group component
     - Cell Size: 85x85
     - Spacing: 5x5
     - Start Corner: Upper Left
     - Start Axis: Horizontal
     - Child Alignment: Upper Left
     - Constraint: Fixed Column Count (4)

4. Create a Text element for instructions
   - "Drag items to your inventory to collect them"
   - Position at bottom of panel

## 3. Create Item Tooltip

1. Create a UI Panel
   - Name it "ItemTooltip"
   - Set anchors to middle-center
   - Size approximately 200x100 pixels
   - Background color: dark with slight transparency

2. Add TextMeshPro component
   - Name it "TooltipText"
   - Rich text enabled
   - Word wrapping enabled
   - Font size: 14
   - Text alignment: upper left
   - Padding: 10px all around

## 4. Set Up ChestInteractionSystem

1. Create a new empty GameObject
   - Name it "ChestInteractionSystem"

2. Add the ChestInteractionSystem script
   - Set Max Interaction Distance: 3
   - Set Chest Layer Mask: Create a new layer for chests and select it
   - Raycast Origin: Assign your player's camera transform
   - Chest Inventory Panel: Assign the ChestInventoryPanel
   - Item Tooltip: Assign the ItemTooltip
   - Tooltip Text: Assign the TooltipText component
   - Chest Slots Parent: Assign the SlotsContainer
   - Inventory Slot Prefab: Assign your ChestItemSlot prefab
   - Chest Inventory Size: 12 (or your preferred number)

## 5. Set Up Chests for Interaction

1. Create a new layer called "Chest"
2. Assign all chest objects to this layer
3. Make sure chest objects have colliders for raycasting
4. Ensure ChestController component is on all chests

## 6. Connect with Player Inventory

1. Make sure your Inventory script is properly set up with public Open/Close methods
2. ChestInteractionSystem will automatically find and use the Inventory component

## Testing

1. Enter play mode
2. Approach a chest that has completed its opening sequence
3. Look at the chest and press E
4. The chest and player inventories should open
5. Test dragging items between inventories
6. Close by pressing Escape

## Troubleshooting

- If raycasts aren't detecting chests, check:
  - Chest layer is included in the ChestInteractionSystem's chestLayerMask
  - Chests have colliders
  - Raycast origin is set correctly
- If UI elements aren't appearing, check:
  - All references are set in the inspector
  - Canvas is set to Screen Space - Overlay
  - EventSystem is present in the scene
