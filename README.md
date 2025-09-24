# last-stand

## Game Over Screen (Setup)

This project includes a minimal Game Over screen flow:

- Place your Game Over screen prefab at `Assets/Resources/Screens/GameOverScreen.prefab`.
  - Add a `Game.UI.Screens.GameOverScreenView` to the root of the prefab.
  - Hook the `Restart` button to the view by assigning it to the `Restart Button` field or wiring the OnClick to call `GameOverScreenView.Restart()`.
- In your gameplay scene, ensure there is a Canvas (or container) with the `Game.UI.UIRoot` component. Screens will be spawned as children of this object.
- Ensure the scene has a `Game.Scopes.GameplayScope` and assign the `UIRoot` reference on it (inspector).
- Add `Game.Gameplay.GameOver.GameOverController` to any scene object and assign all your towers' `TowerHealth` components to its `Towers` list in the inspector.
  - Configure the Lose Condition on the component:
    - AnyTowerDestroyed: Game Over when any listed tower dies.
    - AllTowersDestroyed (default): Game Over only after all listed towers are destroyed.
  - When the lose condition is met, the controller shows the `GameOverScreen` prefab via the ScreenService.

Restart button behavior: loads scene named `Gameplay`. You can override the scene name on `GameOverScreenView` if needed.
