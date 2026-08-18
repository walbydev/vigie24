SELECT column_name, data_type, udt_name 
FROM information_schema.columns 
WHERE table_name = 'sources' 
AND (column_name = 'sourcekind' OR column_name = 'ingestionmethod');
