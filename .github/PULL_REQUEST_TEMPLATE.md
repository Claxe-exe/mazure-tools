## What does this change?

## Checklist

- [ ] `dotnet build` passes with 0 warnings
- [ ] New user-visible text is in **both** `Languages/Strings.English.xaml` and `Strings.Turkish.xaml` (`tools/Test-Localization.ps1` passes)
- [ ] UI code only talks to services through interfaces (no system calls in views / view models)
- [ ] Anything that changes system state shows what will happen and asks first
- [ ] No third-party packages added (or the reason is explained above)
