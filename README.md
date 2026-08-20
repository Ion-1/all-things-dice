# All Things Dice

### Dice Roll Grammar

| Examples                                                 | Compact form                    | Expanded / equivalent forms                                             |
| -------------------------------------------------------- | ------------------------------- | ----------------------------------------------------------------------- |
| Attack roll with +5 modifier                             | `1d20+5`                        | `1d20 + 5`                                                              |
| Attack roll with +5 modifier with disadvantage           | `2d20kl+5`                      | `2d20 keep lowest + 5`                                                  |
| Elven Accuracy attack roll                               | `2d20rrlkh+5`<br>≡ `3d20kh+5`   | `2d20 reroll lowest then keep highest + 5`<br>≡ `3d20 keep highest + 5` |
| Level 3 *Fireball* against Fire Resistance               | `8d6//2`                        | `8d6 // 2`                                                              |
| Three *Magic Missile* bolts with +2 damage on every bolt | `s3(1d4+3)`                     | `sum of 3 (1d4 + 3)`                                                    |
| Roll an ability score: 4d6, drop lowest                  | `4d6dl`<br>≡ `4d6kh3`           | `4d6 drop lowest`<br>≡ `4d6 keep highest 3`                             |
| Multiply two independent 1d4 rolls                       | `p2(1d4)`                       | `product of 2 1d4`                                                      |
