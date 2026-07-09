# Jellyfin Plugin: MoreLikeThis

A plugin to customize the default "More like this" list located at the bottom of Movies and Shows using a score system. This will not require any javascript injection or client side modification.

For example, content will be displayed from highest score to lowest score:

| Property | Score |
| --- | --- |
| Same collection | 100 |
| Same tags | 75 |
| Same studio | 20 |
| Same genre | 10 |

Note: The score value per each property can be customized from the plugin's configuration screen.
Note: Score values can be assigned to logic combinations of properties (e.g. "Same studio" & "Same genre") (e.g. "Same genre" !& "Same studio")

Available properties include:

- Collection
- Tags
- Studio
- Genre

Note: If content genre's dont have a pre-determined weight based on genre order, the following genres will include a 1.1x weight multiplier:

- Animation
- Cartoon
- Childrens

## To-Do

- Update /Services/SimilarityEngine.cs to include:
    - Boolean config values to enable/disable specific properties from the score
    - Config values for the added score value per media property
