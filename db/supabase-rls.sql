-- Supabase only: run after schema.sql.
-- Supabase exposes the public schema through its Data API. Enabling RLS with no policies blocks that
-- access; the API connects as the table owner (postgres), which RLS does not apply to.
ALTER TABLE words ENABLE ROW LEVEL SECURITY;
ALTER TABLE puzzles ENABLE ROW LEVEL SECURITY;
ALTER TABLE puzzle_entries ENABLE ROW LEVEL SECURITY;
ALTER TABLE game_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE word_masteries ENABLE ROW LEVEL SECURITY;
ALTER TABLE player_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE unlocked_achievements ENABLE ROW LEVEL SECURITY;
ALTER TABLE "__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
