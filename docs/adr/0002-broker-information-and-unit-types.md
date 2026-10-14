# Keep unknown broker information nullable

The broker receives incomplete developer-provided details, so descriptive fields must not block unit creation or be replaced with invented zeroes. Use one Units table with a type discriminator, nullable applicable fields and a small name/value extension table instead of three duplicated catalogues or a dynamic form-definition platform; this keeps searching and the shared lifecycle straightforward while allowing details to arrive later. Price absence and management status are independent concepts.
