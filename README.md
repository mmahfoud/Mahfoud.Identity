# Mahfoud.Identity
1. Web API with OpenAPI support.
2. Identity:
	- users table, generic roles (IdentityRole&lt;long&gt;)
	- endpoints: POST /register, GET /verify-email, POST /login, GET /me
3. ToDoLists:
	- to_do_lists table
	- endpoints: GET /api/todolist, GET+POST+PUT+DEL /api/todolist/{id}
	- ensures the list is owned by the current user
3. ToDoItems:
	- to_do_lists table
	- endpoints: 
		- GET /api/todoitem/by-list/{listId}
		- GET /api/todoitem/by-id/{id}
		- POST+PUT+DEL /api/todoitem/{id}
	- ensures the item's list is owned by the current user
	- ensures manipulation commands are performed with one trip to the database
