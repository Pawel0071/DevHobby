# Protos v1
Wersjonowany katalog plików `.proto`. Zasady:

- Nie zmieniamy istniejących numerów pól (field numbers).
- Dodawanie nowych pól wyłącznie z kolejnymi numerami na końcu.
- Usunięte pola: pozostawić numer jako zarezerwowany (nie stosowane tu jeszcze) lub zachować pole jako deprecated.
- Przy zmianie kontraktu wymagającej łamania kompatybilności — utworzyć `v2`.

Test `ProtoFieldNumberTests` w `RPG.IntegrationTests` weryfikuje stabilność numerów dla `quest.proto`. Dodaj analogiczne testy dla innych plików w razie potrzeby.

